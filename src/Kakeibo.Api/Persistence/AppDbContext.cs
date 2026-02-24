using Kakeibo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kakeibo.Api.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletMember> WalletMembers => Set<WalletMember>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<WalletBalance> WalletBalances => Set<WalletBalance>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Naming conventions and NodaTime are configured via DbContextOptionsBuilder in Program.cs
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
