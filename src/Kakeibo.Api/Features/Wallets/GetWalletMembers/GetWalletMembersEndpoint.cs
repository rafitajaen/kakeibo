using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace Kakeibo.Api.Features.Wallets.GetWalletMembers;

public sealed class GetWalletMembersEndpoint : IEndpoint
{
    public sealed record MemberDto(Guid UserId, string Email, bool IsOwner, NodaTime.Instant JoinedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/wallets/{id:guid}/members", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ClaimsPrincipal principal,
        GetWalletMembersHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return TypedResults.Unauthorized();

        var result = await handler.HandleAsync(id, userId, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.StatusCode(403),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
