using Kakeibo.Api.Common.Endpoints;
using NodaTime;

namespace Kakeibo.Api.Features.Admin.ListAdminUsers;

public sealed class ListAdminUsersEndpoint : IEndpoint
{
    public sealed record ListAdminUsersResponse(
        Guid Id,
        string Email,
        string Username,
        string? Name,
        string Role,
        bool IsVerified,
        bool IsBlocked,
        bool HasPassword,
        bool HasGoogleId,
        Instant CreatedAt);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/users", HandleAsync)
            .WithTags("Admin")
            .RequireAuthorization("Admin");
    }

    private static async Task<IResult> HandleAsync(
        ListAdminUsersHandler handler,
        int page,
        int pageSize,
        string? search,
        CancellationToken ct)
    {
        var users = await handler.HandleAsync(page, pageSize, search, ct);
        return TypedResults.Ok(users);
    }
}
