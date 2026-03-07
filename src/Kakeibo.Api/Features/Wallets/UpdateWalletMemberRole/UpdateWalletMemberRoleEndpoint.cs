using System.Security.Claims;
using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Wallets.UpdateWalletMemberRole;

public sealed class UpdateWalletMemberRoleEndpoint : IEndpoint
{
    public sealed record UpdateWalletMemberRoleRequest(string Role);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/wallets/{id:guid}/members/{userId:guid}/role", HandleAsync)
            .WithTags("Wallets")
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        Guid userId,
        UpdateWalletMemberRoleRequest request,
        ClaimsPrincipal principal,
        UpdateWalletMemberRoleHandler handler,
        CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId))
        {
            return TypedResults.Unauthorized();
        }

        var result = await handler.HandleAsync(id, userId, request.Role, callerId, ct);
        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error.Code switch
            {
                "not_found" => TypedResults.NotFound(result.Error),
                "forbidden" => TypedResults.StatusCode(403),
                "validation" => TypedResults.BadRequest(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
