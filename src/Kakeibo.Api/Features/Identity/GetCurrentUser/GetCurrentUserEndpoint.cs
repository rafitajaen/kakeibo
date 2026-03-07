using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Identity.GetCurrentUser;

public sealed class GetCurrentUserEndpoint : IEndpoint
{
    public sealed record GetCurrentUserResponse(
        Guid Id,
        string Email,
        string Username,
        string Role,
        bool IsVerified,
        string Currency,
        string? Name,
        bool HasPassword,
        int WeekStartDay,
        int MonthStartDay,
        string CurrencyDecimalSeparator,
        string CurrencyGroupSeparator,
        string CurrencySymbolPosition,
        string CurrencyDisplay);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/me", HandleAsync)
            .WithTags("Identity")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        GetCurrentUserHandler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(httpContext, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "unauthorized" => TypedResults.Unauthorized(),
                "not_found" => TypedResults.NotFound(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
