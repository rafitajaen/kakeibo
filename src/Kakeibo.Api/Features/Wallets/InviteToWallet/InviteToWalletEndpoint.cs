using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Mvc;

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
        [FromHeader(Name = "X-User-Id")] Guid userId,
        InviteToWalletHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, id, userId, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/wallets/{id}/members", result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.Forbid(),
                "conflict" => TypedResults.Conflict(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
