using Kakeibo.Api.Common.Abstractions;
using Kakeibo.Api.Common.Utils;
using Kakeibo.Api.Domain.Entities;
using Kakeibo.Api.Features.Wallets.Events;
using Kakeibo.Api.Infrastructure.Events;
using Kakeibo.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Kakeibo.Api.Features.Wallets.CreateWallet;

// Creates a new personal or shared wallet for the authenticated user.
public sealed class CreateWalletHandler(AppDbContext db, IEventBus eventBus, IClock clock)
{
    // "Other" system category ID (seeded in CategoryConfiguration).
    private static readonly Guid OtherCategoryId = Guid.Parse("10000000-0000-0000-0000-00000000000c");

    public async Task<Result<CreateWalletEndpoint.CreateWalletResponse>> HandleAsync(
        CreateWalletEndpoint.CreateWalletRequest request,
        Guid userId,
        CancellationToken ct)
    {
        // Load user to inherit their currency setting
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Error.NotFound("User not found.");
        }

        // Enforce unique wallet name per user (excluding archived wallets)
        var nameExists = await db.Wallets
            .AnyAsync(w => w.OwnerId == userId && w.Name == request.Name && w.DeletedAt == null, ct);
        if (nameExists)
        {
            return Error.Conflict($"A wallet named '{request.Name}' already exists.");
        }

        var walletType = Enum.Parse<WalletType>(request.Type, ignoreCase: true);

        var wallet = new Wallet
        {
            Name = request.Name,
            Type = walletType,
            OwnerId = userId,
            Currency = user.Currency,
            Icon = request.Icon,
            BackgroundColor = request.BackgroundColor,
            TextColor = request.TextColor
        };

        db.Wallets.Add(wallet);

        // Create WalletBalance atomically alongside the wallet — starts at zero (or InitialBalance).
        var balance = request.InitialBalance > 0m ? request.InitialBalance : 0m;
        db.WalletBalances.Add(new WalletBalance { WalletId = wallet.Id, Balance = balance });

        // If an initial balance is provided, record it as an Income transaction in the same DB transaction.
        if (request.InitialBalance > 0m)
        {
            db.Transactions.Add(new Transaction
            {
                Id = Guid7.NewGuid(),
                Type = TransactionType.Income,
                Amount = request.InitialBalance,
                Description = "Initial balance",
                Date = clock.GetCurrentInstant().InUtc().Date,
                CategoryId = OtherCategoryId,
                WalletId = wallet.Id,
                UserId = userId,
                CreatedAt = clock.GetCurrentInstant(),
                UpdatedAt = clock.GetCurrentInstant()
            });
        }

        // For shared wallets, auto-add the creator as a WalletMember in the same transaction
        if (walletType == WalletType.Shared)
        {
            db.WalletMembers.Add(new WalletMember
            {
                WalletId = wallet.Id,
                UserId = userId
            });
        }

        // Publish event before SaveChangesAsync — fire-and-forget via Channel
        eventBus.Publish(new WalletCreatedEvent
        {
            WalletId = wallet.Id,
            UserId = userId,
            Name = wallet.Name,
            Type = wallet.Type.ToString()
        });

        await db.SaveChangesAsync(ct);

        return new CreateWalletEndpoint.CreateWalletResponse(
            wallet.Id,
            wallet.Name,
            wallet.Type.ToString(),
            wallet.Currency,
            Balance: balance,
            IsArchived: false,
            Icon: wallet.Icon,
            BackgroundColor: wallet.BackgroundColor,
            TextColor: wallet.TextColor,
            wallet.CreatedAt);
    }
}
