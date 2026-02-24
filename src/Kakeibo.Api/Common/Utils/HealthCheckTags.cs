namespace Kakeibo.Api.Common.Utils;

// Health check tag constants used to group endpoints by readiness/liveness
public static class HealthCheckTags
{
    public const string Ready = "ready";
    public const string Live = "live";
}
