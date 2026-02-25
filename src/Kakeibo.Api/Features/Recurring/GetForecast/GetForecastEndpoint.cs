using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace Kakeibo.Api.Features.Recurring.GetForecast;

public sealed class GetForecastEndpoint : IEndpoint
{
    public sealed record ForecastItem(
        Guid PatternId,
        string PatternName,
        string TransactionType,
        decimal Amount,
        string CategoryName,
        string WalletName,
        string Date,
        string Frequency);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Route registered before {id:guid} to avoid ambiguous match with literal "forecast"
        app.MapGet("/api/recurring-patterns/forecast", HandleAsync)
            .WithTags("Recurring")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        GetForecastHandler handler,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        // Clamp days between 1 and 90
        var clampedDays = Math.Clamp(days, 1, 90);
        var result = await handler.HandleAsync(userId, clampedDays, ct);
        return TypedResults.Ok(result);
    }
}
