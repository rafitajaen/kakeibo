namespace Kakeibo.Common.Modules;

// Dispatcher for sync inter-module requests
public interface IModuleClient
{
    Task<TResponse> SendAsync<TResponse>(
        IModuleRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
