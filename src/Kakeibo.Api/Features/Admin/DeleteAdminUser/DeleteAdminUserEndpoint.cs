using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kakeibo.Api.Features.Admin.DeleteAdminUser;

public sealed class DeleteAdminUserEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/admin/users/{id:guid}", HandleAsync)
            .WithTags("Admin")
            .RequireAuthorization("Admin");
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> HandleAsync(
        Guid id,
        DeleteAdminUserHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, ct);
        if (!result.IsSuccess)
        {
            return result.Error.Code == "not_found"
                ? TypedResults.NotFound()
                : TypedResults.BadRequest(result.Error.Message);
        }

        return TypedResults.NoContent();
    }
}
