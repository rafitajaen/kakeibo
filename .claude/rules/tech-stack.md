# Tech Stack

## Prohibited Technologies

| Technology | Reason |
|------------|--------|
| Python | Unified stack on .NET and JS/TS. Scripts must be sh or TypeScript only |
| Firebase | Custom solution with MinIO, PostgreSQL and Redis |
| Supabase | Custom solution with MinIO, PostgreSQL and Redis |
| Kubernetes (K8s) | Unnecessary complexity for the project |
| Biome | Use oxlint + oxfmt — see `src/Kakeibo.Email/CLAUDE.md` |
| Keycloak | Use ASP.NET Core native JWT Bearer — see `src/Kakeibo.Api/CLAUDE.md` |
| RustFS | Abandoned alpha project (no security patches). Use MinIO server instead. The Minio NuGet SDK is still the S3 client library — only the RustFS server is prohibited |
