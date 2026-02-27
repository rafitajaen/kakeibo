namespace Kakeibo.Api.Common.Endpoints;

// Marker interface for endpoints using the REPR pattern
public interface IEndpoint
{
    static abstract void MapEndpoint(IEndpointRouteBuilder app);
}
