using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace Kakeibo.Api.Infrastructure.Caching;

// Caching service using FusionCache with Redis backplane.
public sealed class FusionCacheService(
    IFusionCache fusionCache,
    IOptions<CachingOptions> options) : ICacheService
{
    private readonly CachingOptions _options = options.Value;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var result = await fusionCache.TryGetAsync<T>(key, token: cancellationToken);
        return result.HasValue ? result.Value : default;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var duration = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);
        await fusionCache.SetAsync(key, value, duration, token: cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await fusionCache.RemoveAsync(key, token: cancellationToken);
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var duration = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);
        return await fusionCache.GetOrSetAsync(key, factory, duration, token: cancellationToken);
    }
}
