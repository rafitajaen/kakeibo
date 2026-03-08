using Kakeibo.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Kakeibo.Api.Features.Admin.CreateAdminUser;

public sealed class CreateAdminUserEndpoint : IEndpoint
{
    public sealed record CreateAdminUserRequest(
        string Email,
        string Password,
        string Currency,
        string? Name,
        string Role);

    public sealed record CreateAdminUserResponse(Guid Id, string Email, string Username, string Role);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/users", HandleAsync)
            .WithTags("Admin")
            .RequireAuthorization("Admin")
            .WithValidation<CreateAdminUserRequest>();
    }

    private static async Task<Results<Created<CreateAdminUserResponse>, Conflict<string>>> HandleAsync(
        CreateAdminUserRequest request,
        CreateAdminUserHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/admin/users/{result.Value.Id}", result.Value)
            : TypedResults.Conflict(result.Error.Message);
    }
}
