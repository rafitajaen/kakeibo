# Storage Migration: RustFS → MinIO

## Background

RustFS was originally chosen as an Apache 2.0 alternative to MinIO after MinIO changed its server
license to AGPL-3.0. The rationale was to avoid AGPL obligations by using an alternative with a
more permissive license. However, RustFS stalled at `v1.0.0-alpha.83` with no further releases
and no security patches. It is effectively an abandoned project.

MinIO remains the industry standard for self-hosted S3-compatible object storage. It is actively
maintained, production battle-tested at scale, and the reference implementation that all S3-
compatible alternatives (including RustFS) try to mimic.

## Why MinIO Is Acceptable

- **Internal only**: MinIO is never exposed to the internet. It runs inside the Docker Compose
  network and is accessed only by `kakeibo-api` via the internal `minio:9000` hostname.
- **AGPL-3.0 does not affect internal use**: AGPL copyleft obligations are triggered when you
  distribute modified versions of the software or run it as a network service for third parties.
  Running MinIO as an internal infrastructure component for a private application does not trigger
  any AGPL obligations. The Kakeibo codebase does not link MinIO's code — it only communicates
  with it over HTTP (S3 API).
- **The MinIO NuGet SDK is MIT-licensed**: `Minio` (v7.0.0) is the S3 client library used by
  `StorageService.cs`. It is MIT-licensed and unaffected by any server license.
- **Production battle-tested**: MinIO is deployed at petabyte scale by Fortune 500 companies.
  Unlike RustFS alpha, it has a defined security disclosure process and regular patch releases.

## Existing Abstraction

`IStorageService` at `src/Kakeibo.Api/Infrastructure/Storage/IStorageService.cs` is the
provider-agnostic contract. No feature code depends on `StorageService` directly — all calls go
through the interface. This means the storage provider can be replaced without touching any
feature handler.

The `StorageService.cs` implementation communicates with the MinIO SDK exclusively via the
S3-compatible HTTP API. Since both RustFS and MinIO implement the same API surface, no code
changes were needed for this migration — only infrastructure configuration.

## Migration Changes

This migration required **zero code changes**. All changes were infrastructure-only:

| File | Change |
|------|--------|
| `docker-compose.yml` | `rustfs` service → `minio` service; `RUSTFS_ROOT_*` → `MINIO_ROOT_*`; removed `rustfs-logs` volume |
| `.env.example` | Updated comment from RustFS to MinIO |
| `src/Kakeibo.Api/.env.local.example` | Updated comment from RustFS to MinIO |
| `.claude/rules/tech-stack.md` | Removed MinIO server from prohibited; added RustFS to prohibited |
| `.claude/rules/infrastructure.md` | Replaced all RustFS references with MinIO |

## How to Swap Storage Providers in the Future

If a different provider is ever needed (e.g., Azure Blob Storage, AWS S3, Cloudflare R2):

1. Implement `IStorageService` with a new class:

```csharp
// src/Kakeibo.Api/Infrastructure/Storage/AzureBlobStorageService.cs
public sealed class AzureBlobStorageService(BlobServiceClient client) : IStorageService
{
    public async Task<string> UploadAsync(string bucket, string key, Stream content, CancellationToken ct)
    {
        // Azure Blob implementation
    }
    // ... other interface methods
}
```

2. Update the DI registration in `Program.cs` (one-line change):

```csharp
// Before
builder.Services.AddSingleton<IStorageService, StorageService>();

// After
builder.Services.AddSingleton<IStorageService, AzureBlobStorageService>();
```

3. Remove `StorageOptions` usage and add the new provider's options class. No feature code changes.

## Security Considerations

- MinIO ports `9000` (API) and `9001` (Console) are marked `## REMOVE ON PRODUCTION` in
  `docker-compose.yml`. They must not be forwarded in production deployments.
- In production, `kakeibo-api` connects to MinIO via the Docker internal network (`minio:9000`).
- Admin access to the MinIO Console in production must go through an SSH tunnel only.
- `STORAGE_ACCESS_KEY` and `STORAGE_SECRET_KEY` in `.env` are the MinIO root credentials.
  Rotate them after initial setup using the MinIO Console or `mc` CLI.

## Environment Variable Changes

| Before | After |
|--------|-------|
| `RUSTFS_ROOT_USER: ${STORAGE_ACCESS_KEY}` | `MINIO_ROOT_USER: ${STORAGE_ACCESS_KEY}` |
| `RUSTFS_ROOT_PASSWORD: ${STORAGE_SECRET_KEY}` | `MINIO_ROOT_PASSWORD: ${STORAGE_SECRET_KEY}` |

The `.env` variable names (`STORAGE_ACCESS_KEY`, `STORAGE_SECRET_KEY`) are unchanged —
they are provider-agnostic by design.

## Verification Checklist

1. `docker compose down && docker compose up -d` — MinIO starts on ports 9000/9001
2. Open http://localhost:9001 — MinIO Console loads (login with `STORAGE_ACCESS_KEY` / `STORAGE_SECRET_KEY`)
3. `bun run api:build` — no compilation errors
4. `bun run api:test` — all tests pass
5. `dotnet run` with `.env.local` pointing to `localhost:9000` — API connects to MinIO
6. Upload a test file via the API — file appears in MinIO Console under the correct bucket
