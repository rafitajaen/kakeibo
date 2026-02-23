# Kakeibo

Personal finance and shared expense management platform inspired by traditional Japanese budgeting. Modular monolith monorepo with event-driven architecture. Current phase: **Phase 0 (base infrastructure) — partially complete**.

**Language:** Always respond in Spanish. All code, commits, PRs, code comments, and documentation must be in English.

---

## Critical Rules

- **TDD**: When building features or fixing bugs, follow TDD (red-green-refactor). Invoke the `/kakeibo-testing` skill for test structure, infrastructure patterns, and coverage decisions.
- **Documentation**: After any change that introduces or modifies a pattern, flow, or architectural concept, document it in the appropriate `.claude/rules/` file (architecture.md, technical-debt.md, knowledge.md, etc.).
- **README.md**: Keep the root `README.md` up to date at each phase milestone: update the phase status line, project structure (new modules or services), key commands, and service URLs whenever a phase is started or completed.
- **No prohibited technologies**: See `.claude/rules/tech-stack.md` and the Prohibited section below.
- **No cross-module references**: Module A never references Module B's project. Enforced by architecture tests.
- **NuGet versions**: All versions in `Directory.Packages.props`. Never `Version="x.x.x"` in `.csproj`.
- **Solution format**: `Kakeibo.slnx` (not `.sln`).
- **Guid7**: Use `Guid7.NewGuid()` for entity IDs. `Guid.CreateVersion7()` is PROHIBITED.
- **NodaTime**: Use `Instant`, `LocalDate`, `LocalTime`. Never `DateTime` or `DateTimeOffset`.
- **Never `dotnet format`**: Prohibited in all forms (`bun run api:format`, `dotnet format`, etc.). The user runs formatting manually. See mandatory.md Rule 7.
- **Sequential execution**: NEVER run build, test, or format commands in parallel (no parallel subagents). Execute them sequentially to avoid saturating the host OS.

---

## Tech Stack

> Full tech stack: `.claude/rules/tech-stack.md`

Key technologies: .NET 10 Minimal APIs, EF Core + PostgreSQL 18, FusionCache + Redis,
Outbox Pattern, Hangfire (background jobs), Vue 3 Composition API + Pinia + shadcn-vue,
Bun, xUnit v3 + Testcontainers / Vitest + Playwright.

---

## Prohibited Technologies

| Prohibited | Use instead |
|------------|-------------|
| Python scripts | sh or TypeScript scripts only |
| EF Core InMemory, SQLite in-memory | Testcontainers with real PostgreSQL |
| MediatR | Plain handler classes, no CQRS interfaces |
| AutoMapper | Manual mapping or extension methods |
| `DateTime` / `DateTimeOffset` | NodaTime (`Instant`, `LocalDate`, `LocalTime`) |
| `Guid.CreateVersion7()` | `Guid7.NewGuid()` for entity IDs |
| Swagger | Scalar |
| ESLint, Prettier, Biome | oxlint, oxfmt |
| Options API (Vue) | Composition API with `<script setup>` |
| Moq | NSubstitute |
| Newtonsoft.Json | System.Text.Json (native .NET) |
| Quartz.NET | Hangfire + Hangfire.PostgreSql |
| `lucide-vue-next` | `@hugeicons/vue` + `@hugeicons/core-free-icons` |
| `FluentAssertions` | Use xUnit v3 native `Assert.*` methods manually |
| `npx` | `bunx` (or `bunx --bun` when Bun runtime is required) |

> Full list: `.claude/rules/tech-stack.md`

---

## Git Conventions

**Repository:** GitHub at `https://github.com/rafitajaen/kakeibo.git`. Use `gh` (GitHub CLI) for PR/issue operations.

### Commit Format

```
type(scope): description
```

- **Types**: feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert
- **Scopes (scope-enum):** `app` · `api` · `email` · `docs` · `infra` · `deps` · `release` · `skills` · `roadmap`
- When adding a new project, add its scope to `commitlint.config.ts` and update this list (see mandatory.md Rule 6).
- Scope is optional but recommended
- Max subject length: 100 characters
- Language: English

### Branches

- `main` — production-ready
- `feat/phase-N` — phase development branches
- `feat/{scope}-{description}` — feature branches

### Semantic Release

This project uses [semantic-release](https://semantic-release.gitbook.io/) for automated versioning and release management.

**Key points:**
- Runs automatically on every push to `main`
- Analyzes conventional commits to calculate the next version
- Updates `CHANGELOG.md`, `package.json`, and creates GitHub Releases
- **Never manually edit versions, tags, or CHANGELOG.md**

**Version calculation:**
- `feat`: Minor bump (0.x.0) → `v0.1.0` to `v0.2.0`
- `fix`, `perf`, `revert`, `refactor`: Patch bump (0.0.x) → `v0.2.0` to `v0.2.1`
- Breaking changes: Major bump (x.0.0) → `v0.2.1` to `v1.0.0`
- `docs`, `style`, `chore`, `test`, `build`, `ci`: No release

**Initial setup (already done):**
- ✅ Tag `v0.0.0` created in commit "Initial commit"
- ✅ GH_TOKEN configured in GitHub Secrets
- ✅ Workflow `.github/workflows/release.yml` enabled
- ✅ Configuration `.releaserc.json` in place

**For developers:**
- Follow conventional commits format (enforced by commitlint)
- Don't worry about versioning — semantic-release handles it
- After merge to `main`, verify the new release in GitHub Releases
- Check `CHANGELOG.md` to see your feature documented

**Detailed documentation:** See `.claude/rules/ci.md` for complete CI/CD and semantic-release reference.

### Git CLI Syntax

- **`git diff` flags go BEFORE paths:** `git diff --stat path/to/file` (NOT `git diff path/to/file --stat`). Git treats options after non-option arguments as errors.

### Pre-commit Hooks (lefthook)

- **commit-msg**: commitlint (conventional commits)
- **pre-commit** (parallel): oxlint + oxfmt on staged `.ts/.tsx/.vue/.js/.jsx/.css/.json` files

---

## Commands

All scripts are defined in `package.json` (root). Run with `bun run <script>`.

**Currently available:** `api:*`, `email:*`, `docker:*`, `check:*`, `dev:*`

**Future (when implemented):** `app:*` commands will be added when `sites/Kakeibo.App` is created.

**CLI tools:** Use `bunx` instead of `npx` for all one-off CLI invocations. Add `--bun` when the tool must run under the Bun runtime. See mandatory.md Rule 9.

**EF Core migrations:**
```bash
dotnet ef migrations add <Name> \
  --project src/Kakeibo.Modules.<Module> \
  --startup-project src/Kakeibo.Api \
  --context <Module>DbContext \
  --output-dir Persistence/Migrations
```

---

## C# Backend Conventions

> Full naming rules: `.claude/rules/technical-debt.md` (TD-009–TD-013) — Full architecture: `.claude/rules/architecture.md`

### Architecture

Vertical Slices + Screaming Architecture + Modular Monolith. Full spec: `.claude/rules/architecture.md` — module anatomy, inter-module communication (`IModuleClient` / `IModuleEventBus`), dependency rules, DI registration pattern.

Each feature lives in `src/Kakeibo.Modules.{Module}/Features/{Operation}/` with up to 3 files: `{Op}Endpoint.cs`, `{Op}Handler.cs`, `{Op}Validator.cs`. Handlers are plain classes auto-registered by Scrutor. No MVC controllers, no FastEndpoints, no MediatR.

### Modules

8 modules total:
- **Core:** Identity, Notifications, Auditing
- **Business:** Wallets (includes Collaboration features), Transactions (includes Categories), Budgets, Goals, Recurring

### Quick Reference

**EF Core:** `UseSnakeCaseNamingConvention()`, `UseNodaTime()`, never `DateTime`.
**Passwords:** `PasswordHasher` (PBKDF2-SHA512). Never BCrypt or Argon2id.
**JSON:** `DefaultSerializer.Options` (camelCase, nulls ignored, NodaTime support).
**IDs:** `Guid7.NewGuid()` for entities. Regular `Guid` allowed elsewhere.
**Constants:** Hardcoded enumerator strings → `public static class` with `public const string`. Config sections → `{Name}Options` with `const string SectionName`.
**Naming:** `{Op}Endpoint`, `{Op}Handler`, `{Op}Validator`, nested `{Op}Request`/`{Op}Response`. Never `*Dto` outside `Kakeibo.Contracts`.
**Endpoint URLs:** Resource → `/api/{resource}` (REST CRUD). Action → `POST /api/{resource}/{id}/{verb}`. Self-service → `/api/users/me/{resource}`.
**InternalsVisibleTo:** Every `src/` project must expose internals to its test project.
**Comments:** Non-trivial methods need a summary comment above the signature + inline `//` for non-obvious logic.
**C# Style:** File-scoped namespaces, `Nullable` enabled, `TreatWarningsAsErrors` enabled, primary constructors required (`IDE0290`).

---

## Frontend Conventions

> Full rules: `.claude/rules/technical-debt.md` (TD-017–TD-018) — i18n rule: `.claude/rules/mandatory.md` (Rule 5)

**Note:** `sites/Kakeibo.App` does not exist yet (planned for future phases). These conventions apply when it is implemented.

**Components:** `<script setup lang="ts">` always. SFC order: script → template → style scoped. PascalCase filenames.
**Pinia:** Setup function style (`defineStore('name', () => {...})`). `ref()` for state, `computed()` for getters, functions for actions.
**UI:** shadcn-vue first — check registry before building custom components (`bunx --bun shadcn-vue@latest add <component>` from the project folder). Icons: `HugeiconsIcon` wrapper + `@hugeicons/core-free-icons`. Forms: VeeValidate + Zod. Dates: date-fns.
**i18n:** Every user-visible string must use `t('key')`. Keys must exist in both `locales/en.json` and `locales/es.json`. See mandatory.md Rule 5.
**Imports:** `@/` for cross-directory. Only `./file` (same dir) may use relative paths.
**Testing:** Unit → Vitest (`test/components/{Name}.spec.ts`). E2E → Playwright (`e2e/{feature}.spec.ts`).

---

## Workflow

### Before Starting Any Task

1. Read the current phase documentation in `.claude/roadmap/` (canonical roadmap reference)
2. Check `.claude/rules/tech-stack.md` for approved technologies
3. Verify the task doesn't use any prohibited technology
4. Check this file's conventions for the relevant layer (backend/frontend)

### Creating an API Endpoint

1. Create feature folder: `src/Kakeibo.Modules.{Module}/Features/{Operation}/`
2. Create `{Op}Endpoint.cs` implementing `IEndpoint` with nested `{Op}Request`/`{Op}Response` records
3. Implement static `MapEndpoint(IEndpointRouteBuilder app)` to register the route
4. Create `{Op}Handler.cs` — plain class with `HandleAsync` method (injected via DI)
5. Create `{Op}Validator.cs` inheriting `AbstractValidator<T>` with FluentValidation rules
6. Use `Guid7` for IDs, `NodaTime` for dates, `DefaultSerializer.Options` for JSON
7. Create test: `tests/Kakeibo.Modules.{Module}.Tests/Features/{Op}/{Op}Tests.cs`
8. Run: `bun run api:test:unit`

### Creating a Vue Component

**Note:** Apply when `sites/Kakeibo.App` is implemented.

1. Create file with PascalCase: `sites/Kakeibo.App/components/{Name}.vue`
2. Use `<script setup lang="ts">` — never Options API
3. Follow SFC order: script → template → style scoped
4. Use shadcn-vue components, Tailwind CSS v4 for styling
5. Create test: `sites/Kakeibo.App/test/components/{Name}.spec.ts`
6. Run: `bun run app:lint:check && bun run app:test:unit`

### Creating a Pinia Store

**Note:** Apply when `sites/Kakeibo.App` is implemented.

1. Create file: `sites/Kakeibo.App/stores/{name}.ts`
2. Use setup function style: `defineStore('name', () => { ... })`
3. Export as `use{Name}Store`
4. Use `ref()` for state, `computed()` for getters, functions for actions
5. Follow pattern in `stores/counter.ts` (if/when created)

### Adding a NuGet Package

1. Add version to `Directory.Packages.props`: `<PackageVersion Include="..." Version="..." />`
2. Add reference in `.csproj` WITHOUT version: `<PackageReference Include="..." />`
3. Run: `bun run api:restore && bun run api:build`

### Before Committing

1. Backend: `bun run api:test:unit && bun run api:test:arch`
2. Frontend (when exists): `bun run app:format && bun run app:lint && bun run app:test:unit`
3. Email (if changed): `bun run email:format`
4. Re-stage any files modified by the formatters before committing: `git add <files>`
5. Documentation: Update or create the corresponding page in `.claude/rules/` if the change introduces or modifies a pattern, flow, or architectural concept.
6. README.md: Update the phase status, project structure, and any new commands or service URLs if the change marks a phase milestone or adds a new module/service.
7. Commit with conventional format: `type(scope): description`
8. Pre-commit hooks will run automatically (commitlint + oxlint + oxfmt on staged files)

---

## Key Patterns (Reference Implementations)

Read these files when implementing similar functionality:

| Pattern | File | What it demonstrates |
|---------|------|---------------------|
| IEndpoint pattern | `src/Kakeibo.Common/Endpoints/IEndpoint.cs` | Minimal API REPR pattern interface |
| Validation filter | `src/Kakeibo.Common/Endpoints/ValidationFilter.cs` | Generic FluentValidation endpoint filter |
| Endpoint scanning | `src/Kakeibo.Common/Endpoints/EndpointExtensions.cs` | Assembly scanning for IEndpoint |
| DI extensions | `src/Kakeibo.Api/DependencyInjection/` | Modular DI registration (one file per concern) |
| Password hashing | `src/Kakeibo.Common/Utils/PasswordHasher.cs` | PBKDF2-SHA512, salt generation, constant-time verify |
| JSON serialization | `src/Kakeibo.Common/Utils/DefaultSerializer.cs` | camelCase, null handling, NodaTime support |
| ID generation | `src/Kakeibo.Common/Utils/Guid7.cs` | UUIDv7 type-safe wrapper |
| Pinia store | `sites/Kakeibo.App/stores/counter.ts` | Setup function style, ref + computed + functions (when created) |
| String constants | `src/Kakeibo.Common/Utils/CharSets.cs` | Static class with `public const string` fields |
| Module registration | `src/Kakeibo.Modules.{Module}/{Module}ModuleRegistration.cs` | DI + endpoint registration per module |
| Module anatomy | `.claude/rules/architecture.md` | Modular monolith structure, module anatomy, inter-module communication |

---

## Reference Documentation

| File | Content |
|------|---------|
| `.claude/roadmap/roadmap.md` | Canonical roadmap — phases, dependencies, implementation strategy |
| `.claude/rules/platform.md` | Canonical business domain: 8 modules, user model, wallets, transactions, collaboration |
| `.claude/rules/tech-stack.md` | All technologies and prohibited list |
| `.claude/rules/architecture.md` | Modular monolith structure, dependency rules, module anatomy |
| `.claude/rules/technical-debt.md` | Technical debt rules, code patterns to avoid, audit criteria |
| `.claude/rules/mandatory.md` | Business invariants: Admin role, Dockerfiles, i18n, CI registration, Testcontainers |
| `.claude/rules/knowledge.md` | Knowledge base KB-001–KB-009: gotchas and lessons learned |
| `.claude/rules/infrastructure.md` | Docker Compose, Dockerfiles, env strategy, CI/CD pipeline |
| `.claude/rules/overview.md` | Platform philosophy, core functionality, key concepts, main flows |
| `.claude/rules/constraints.md` | Business constraints and limits (transaction amounts, wallet limits, etc.) |
