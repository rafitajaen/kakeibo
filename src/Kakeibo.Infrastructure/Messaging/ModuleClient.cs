using Kakeibo.Common.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace Kakeibo.Infrastructure.Messaging;

// Dispatcher for synchronous inter-module requests.
// Resolves IModuleRequestHandler<,> from DI via reflection and dispatches in-process.
public sealed class ModuleClient(IServiceProvider serviceProvider) : IModuleClient
{
    public async Task<TResponse> SendAsync<TResponse>(
        IModuleRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var handlerType = typeof(IModuleRequestHandler<,>).MakeGenericType(requestType, responseType);

        var handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException(
                $"No handler registered for request type '{requestType.Name}'. " +
                $"Ensure the module that owns this request has registered an IModuleRequestHandler<{requestType.Name}, {responseType.Name}>.");

        var method = handlerType.GetMethod(nameof(IModuleRequestHandler<IModuleRequest<TResponse>, TResponse>.HandleAsync))
            ?? throw new InvalidOperationException($"HandleAsync method not found on handler type '{handlerType.Name}'.");

        var task = method.Invoke(handler, [request, cancellationToken]) as Task<TResponse>
            ?? throw new InvalidOperationException($"HandleAsync did not return a Task<{responseType.Name}>.");

        return await task;
    }
}
