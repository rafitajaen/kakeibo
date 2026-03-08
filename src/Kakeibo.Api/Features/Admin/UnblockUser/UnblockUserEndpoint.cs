using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kakeibo.Api.Features.Admin.UnblockUser;

public sealed class UnblockUserEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/users/{id:guid}/unblock", HandleAsync)
            .WithTags("Admin")
            .RequireAuthorization("Admin");
    }

    private static async Task<Results<NoContent, NotFound>> HandleAsync(
        Guid id,
        UnblockUserHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, ct);
        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}
