using Kakeibo.Api.Common.Endpoints;

namespace Kakeibo.Api.Features.Identity.RegisterUser;

public sealed class RegisterUserEndpoint : IEndpoint
{
    public sealed record RegisterUserRequest(string Email, string Password, string Currency = "EUR");
    public sealed record RegisterUserResponse(Guid Id, string Email);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", HandleAsync)
            .WithTags("Identity")
            .WithValidation<RegisterUserRequest>();
    }

    private static async Task<IResult> HandleAsync(
        RegisterUserRequest request,
        RegisterUserHandler handler,
        CancellationToken ct)
    {
        var result = await handler.HandleAsync(request, ct);
        return result.IsSuccess
            ? TypedResults.Created($"/api/auth/me", result.Value)
            : result.Error.Code switch
            {
                "conflict" => TypedResults.Conflict(result.Error),
                _ => TypedResults.Problem(result.Error.Message, statusCode: 500)
            };
    }
}
