var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/health", () => "OK");

app.Run();

// Required for WebApplicationFactory<Program> in tests
public partial class Program;
