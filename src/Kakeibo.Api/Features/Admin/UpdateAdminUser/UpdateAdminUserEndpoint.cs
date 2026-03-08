using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kakeibo.Api.Features.Admin.UpdateAdminUser;

public sealed class UpdateAdminUserEndpoint : IEndpoint
{
    public sealed record UpdateAdminUserRequest(
        string? Name,
        string? Role);

    public sealed record UpdateAdminUserResponse(
        Guid Id,
        string Email,
        string Username,
        string? Name,
        string Role);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/admin/users/{id:guid}", HandleAsync)
            .WithTags("Admin")
            .RequireAuthorization("Admin")
            .WithValidation<UpdateAdminUserRequest>();
    }

    private static async Task<Results<Ok<UpdateAdminUserResponse>, NotFound>> HandleAsync(
        Guid id,
        UpdateAdminUserRequest request,
        UpdateAdminUserHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(id, request, ct);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.NotFound();
    }
}
