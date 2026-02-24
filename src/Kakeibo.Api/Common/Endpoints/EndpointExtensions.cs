using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace Kakeibo.Api.Common.Endpoints;

// Extension methods for registering IEndpoint implementations via assembly scanning
public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app, Assembly assembly)
    {
        var endpointTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IEndpoint)));

        foreach (var type in endpointTypes)
        {
            var method = type.GetMethod(nameof(IEndpoint.MapEndpoint), BindingFlags.Public | BindingFlags.Static);
            method?.Invoke(null, [app]);
        }

        return app;
    }
}
