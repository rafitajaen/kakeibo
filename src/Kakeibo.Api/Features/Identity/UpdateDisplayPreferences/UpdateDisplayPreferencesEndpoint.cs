using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kakeibo.Api.Features.Identity.UpdateDisplayPreferences;

public sealed class UpdateDisplayPreferencesEndpoint : IEndpoint
{
    public sealed record UpdateDisplayPreferencesRequest(
        int? WeekStartDay,
        int? MonthStartDay,
        string? CurrencyDecimalSeparator,
        string? CurrencyGroupSeparator,
        string? CurrencySymbolPosition,
        string? CurrencyDisplay);

    public sealed record UpdateDisplayPreferencesResponse(
        int WeekStartDay,
        int MonthStartDay,
        string CurrencyDecimalSeparator,
        string CurrencyGroupSeparator,
        string CurrencySymbolPosition,
        string CurrencyDisplay);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/users/me/display-preferences", HandleAsync)
            .WithTags("Identity")
            .RequireAuthorization()
            .WithValidation<UpdateDisplayPreferencesRequest>();
    }

    private static async Task<IResult> HandleAsync(
        UpdateDisplayPreferencesRequest request,
        UpdateDisplayPreferencesHandler handler,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, httpContext, ct);
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
