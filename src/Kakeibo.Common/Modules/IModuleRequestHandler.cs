namespace Kakeibo.Common.Modules;

// Handler for sync inter-module requests
public interface IModuleRequestHandler<in TRequest, TResponse>
    where TRequest : IModuleRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
