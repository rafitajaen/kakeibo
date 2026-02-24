using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace Kakeibo.Api.Features.Wallets.InviteToWallet;

public sealed class InviteToWalletEndpoint : IEndpoint
{
    public sealed record InviteToWalletRequest(string InviteeEmail);
    public sealed record InviteToWalletResponse(Guid InvitationId, string Code, string InviteeEmail);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/wallets/{id:guid}/invite", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization()
            .WithValidation<InviteToWalletRequest>();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        InviteToWalletRequest request,
        ClaimsPrincipal principal,
        InviteToWalletHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return TypedResults.Unauthorized();

        var result = await handler.HandleAsync(request, id, userId, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/wallets/{id}/members", result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.StatusCode(403),
                "conflict" => TypedResults.Conflict(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
