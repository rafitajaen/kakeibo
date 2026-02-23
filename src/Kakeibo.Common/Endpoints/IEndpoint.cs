using Microsoft.AspNetCore.Routing;

namespace Kakeibo.Common.Endpoints;

// Marker interface for endpoints
public interface IEndpoint
{
    static abstract void MapEndpoint(IEndpointRouteBuilder app);
}
