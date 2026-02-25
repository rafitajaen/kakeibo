using DotNetEnv;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using NodaTime;
using Kakeibo.Api.Common.Endpoints;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Features.Recurring.Jobs;
using Kakeibo.Api.Infrastructure.Audit;
using Kakeibo.Api.Infrastructure.Auth;
using Kakeibo.Api.Infrastructure.Caching;
using Kakeibo.Api.Infrastructure.Email;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Infrastructure.HealthChecks;
using Kakeibo.Api.Infrastructure.Storage;
using Kakeibo.Api.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using ZiggyCreatures.Caching.Fusion;

// Load environment files for local development (silently ignored in Docker/production)
// ../../.env contains credential primitives; .env.local assembles connection strings with ${VAR} interpolation
Env.Load("../../.env");
Env.Load(".env.local");

var builder = WebApplication.CreateBuilder(args);

// --- OpenAPI ---
builder.Services.AddOpenApi();

// --- Authentication: JWT Bearer via HttpOnly cookies ---
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtSettings = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException($"Missing required configuration section '{JwtOptions.SectionName}'.");

var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = jwtKey,
            ClockSkew = TimeSpan.Zero
        };

        // Read access token from HttpOnly cookie instead of Authorization header
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                ctx.Token = ctx.Request.Cookies["access_token"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// --- Persistence: PostgreSQL + EF Core ---
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? throw new InvalidOperationException("Missing required connection string 'PostgreSQL'.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.UseNodaTime())
           .UseSnakeCaseNamingConvention());

// --- Events: in-process async event bus ---
builder.Services.AddSingleton<ChannelEventBus>();
builder.Services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<ChannelEventBus>());
builder.Services.AddHostedService<EventDispatcher>();

// --- Caching: FusionCache (in-memory, Redis backplane optional) ---
builder.Services.Configure<CachingOptions>(builder.Configuration.GetSection(CachingOptions.SectionName));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));

var cachingDefaults = builder.Configuration.GetSection(CachingOptions.SectionName).Get<CachingOptions>()
    ?? new CachingOptions();
var defaultCacheMinutes = cachingDefaults.DefaultExpirationMinutes;

builder.Services.AddFusionCache()
    .WithDefaultEntryOptions(o => o.Duration = TimeSpan.FromMinutes(defaultCacheMinutes))
    .WithSystemTextJsonSerializer();

builder.Services.AddSingleton<ICacheService, FusionCacheService>();

// --- Email ---
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<EmailRendererOptions>(builder.Configuration.GetSection(EmailRendererOptions.SectionName));
builder.Services.AddHttpClient();
builder.Services.AddTransient<IEmailService, EmailService>();

// --- Storage: S3-compatible (RustFS via MinIO SDK) ---
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddSingleton<IStorageService, StorageService>();

// --- Audit: ClickHouse ---
builder.Services.Configure<ClickHouseOptions>(builder.Configuration.GetSection(ClickHouseOptions.SectionName));
builder.Services.AddSingleton<IAuditService, ClickHouseAuditService>();
builder.Services.AddSingleton<ClickHouseHealthCheck>();

// --- Hangfire: background job processing with PostgreSQL storage ---
builder.Services.AddHangfire(config => config
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

// --- Background jobs: Hangfire recurring jobs ---
builder.Services.AddScoped<GenerateRecurringTransactionsJob>();

// --- Clock: NodaTime singleton used by all handlers and JwtService ---
builder.Services.AddSingleton<IClock>(SystemClock.Instance);

// --- JWT service (used in Identity handlers) ---
builder.Services.AddSingleton<JwtService>();

// --- Feature handlers: auto-scanned by Scrutor ---
builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.Where(t => t.Name.EndsWith("Handler")))
    .AsSelf()
    .WithScopedLifetime());

// --- Event handlers: auto-scanned by Scrutor ---
builder.Services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes.AssignableTo(typeof(IEventHandler<>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());

// --- FluentValidation ---
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// --- Health checks ---
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? string.Empty;

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, tags: [HealthCheckTags.Ready])
    .AddRedis(redisConnectionString, tags: [HealthCheckTags.Ready])
    .Add(new HealthCheckRegistration(
        "clickhouse",
        sp => sp.GetRequiredService<ClickHouseHealthCheck>(),
        HealthStatus.Degraded,
        [HealthCheckTags.Ready]));

var app = builder.Build();

// --- Admin seeding: creates the first admin account if none exists ---
// Reads ADMIN_EMAIL and ADMIN_PASSWORD from environment variables (set in .env for dev, secrets for prod).
// Silently skipped if either variable is missing — safe to deploy without pre-seeding.
var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
{
    using var seedScope = app.Services.CreateScope();
    var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();

    var hasAdmin = await seedDb.Users.AnyAsync(u => u.Role == UserRole.Admin);
    if (!hasAdmin)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var admin = new User
        {
            Email = adminEmail.ToLowerInvariant(),
            PasswordHash = PasswordHasher.HashPassword(adminPassword),
            Role = UserRole.Admin,
            IsVerified = true,
            VerifiedAt = now,
            Currency = "EUR"
        };
        seedDb.Users.Add(admin);
        await seedDb.SaveChangesAsync();
    }
}

// --- Middleware pipeline ---
app.UseAuthentication();
app.UseAuthorization();

// --- Endpoints ---
app.MapOpenApi();
app.MapScalarApiReference();
app.MapHealthChecks("/health");
app.MapEndpoints(typeof(Program).Assembly);
app.MapHangfireDashboard();

// --- Recurring jobs ---
RecurringJob.AddOrUpdate<GenerateRecurringTransactionsJob>(
    "generate-recurring-transactions",
    job => job.ExecuteAsync(),
    "0 1 * * *"); // Daily at 01:00 UTC

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program;
