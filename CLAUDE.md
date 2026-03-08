# Kakeibo

Personal finance and shared expense management platform inspired by traditional Japanese budgeting. Simple monolith monorepo with event-driven architecture. Current phase: **Phase 8 complete. MVP.**

**Language:** Always respond in Spanish. All code, commits, PRs, code comments, and documentation must be in English.

---

## Critical Rules

- **TDD**: When building features or fixing bugs, follow TDD (red-green-refactor). Invoke the `/kakeibo-testing` skill for test structure, infrastructure patterns, and coverage decisions.
- **Documentation**: After any change that introduces or modifies a pattern, flow, or architectural concept, document it in the appropriate `.claude/` documentation file.
- **README.md**: Keep the root `README.md` up to date at each phase milestone: update the phase status line, project structure, key commands, and service URLs whenever a phase is started or completed.
- **No prohibited technologies**: Check the Tech Stack section in the project-specific `CLAUDE.md` (API or App) for the prohibited technologies list.
- **Guid7**: Use `Guid7.NewGuid()` for entity IDs. `Guid.CreateVersion7()` is PROHIBITED.
- **NodaTime**: Use `Instant`, `LocalDate`, `LocalTime`. Never `DateTime` or `DateTimeOffset`.
- **Sequential execution**: NEVER run build, test, or format commands in parallel (no parallel subagents). Execute them sequentially to avoid saturating the host OS.

---

## Git Conventions

**Repository:** GitHub at `https://github.com/rafitajaen/kakeibo.git`. Use `gh` (GitHub CLI) for PR/issue operations.

### Commit Format

```
type(scope): description
```

- **Types**: feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert
- **Scopes (scope-enum):** `app` · `api` · `android` · `email` · `docs` · `infra` · `deps` · `release` · `skills` · `roadmap`
- When adding a new project, add its scope to `commitlint.config.ts` and update this list.
- Scope is optional but recommended
- Max subject length: 100 characters
- Language: English

### Branches

- `main` — production-ready
- `feat/phase-N` — phase development branches
- `feat/{scope}-{description}` — feature branches

### Semantic Release

Automated versioning on every push to `main`. Never manually edit versions, tags, or `CHANGELOG.md`.

### Git CLI Syntax

- **`git diff` flags go BEFORE paths:** `git diff --stat path/to/file` (NOT `git diff path/to/file --stat`). Git treats options after non-option arguments as errors.

### Pre-commit Hooks (lefthook)

- **commit-msg**: commitlint (conventional commits)
- **pre-commit** (parallel): oxlint + oxfmt on staged `.ts/.tsx/.vue/.js/.jsx/.css/.json` files

---

## Commands

All scripts are defined in `package.json` (root). Run with `bun run <script>`.

**Currently available:** `api:*`, `app:*`, `email:*`, `docker:*`

**CLI tools:** Use `bunx` instead of `npx` for all one-off CLI invocations. Add `--bun` when the tool must run under the Bun runtime.

**EF Core migrations:**
```bash
dotnet ef migrations add <Name> \
  --project src/Kakeibo.Api \
  --startup-project src/Kakeibo.Api \
  --context AppDbContext \
  --output-dir Persistence/Migrations
```

---

## C# Backend Conventions

Each feature: `src/Kakeibo.Api/Features/{Domain}/{Operation}/` → `{Op}Endpoint.cs`, `{Op}Handler.cs`, `{Op}Validator.cs`.
Handlers auto-registered by Scrutor. No MVC controllers, no FastEndpoints, no MediatR.

---

## Frontend Conventions

**Note:** `src/Kakeibo.App` exists. The shell and development tooling are in place but business screens are pending (Phase 1d onwards).

**Components:** `<script setup lang="ts">` always. SFC order: script → template → style scoped. PascalCase filenames.
**Pinia:** Setup function style (`defineStore('name', () => {...})`). `ref()` for state, `computed()` for getters, functions for actions.
**UI:** shadcn-vue first — check registry before building custom components (`bunx --bun shadcn-vue@latest add <component>` from the project folder). Icons: `lucide-vue-next` (import named icon components directly). Forms: VeeValidate + Zod. Dates: date-fns.
**i18n:** Every user-visible string must use `t('key')`. Keys must exist in both `locales/en.json` and `locales/es.json`.
**Imports:** `@/` for cross-directory. Only `./file` (same dir) may use relative paths.
**Testing:** Unit → Vitest (`test/components/{Name}.spec.ts`). E2E → Playwright (`e2e/{feature}.spec.ts`).

---

## Workflow

### Before Starting Any Task

1. Read the current phase documentation in `.claude/roadmap/` (canonical roadmap reference)
2. Check the Tech Stack section in the relevant project `CLAUDE.md` for approved and prohibited technologies
3. Check this file's conventions for the relevant layer (backend/frontend)

### Creating an API Endpoint

1. Create feature folder: `src/Kakeibo.Api/Features/{Domain}/{Operation}/`
2. Create `{Op}Endpoint.cs` implementing `IEndpoint` with nested `{Op}Request`/`{Op}Response` records
3. Implement static `MapEndpoint(IEndpointRouteBuilder app)` to register the route
4. Create `{Op}Handler.cs` — plain class with `HandleAsync` method (injected via DI)
5. Create `{Op}Validator.cs` inheriting `AbstractValidator<T>` with FluentValidation rules
6. Use `Guid7` for IDs, `NodaTime` for dates, `DefaultSerializer.Options` for JSON
7. Create test: `tests/Kakeibo.Tests/Features/{Domain}/{Op}/{Op}Tests.cs`
8. Run: `bun run api:test`

### Creating a Vue Component

**Note:** Apply when `src/Kakeibo.App` is implemented.

1. Create file with PascalCase: `src/Kakeibo.App/components/{Name}.vue`
2. Use `<script setup lang="ts">` — never Options API
3. Follow SFC order: script → template → style scoped
4. Use shadcn-vue components, Tailwind CSS v4 for styling
5. Create test: `src/Kakeibo.App/test/components/{Name}.spec.ts`
6. Run: `bun run app:lint:check && bun run app:test:unit`

### Creating a Pinia Store

**Note:** Apply when `src/Kakeibo.App` is implemented.

1. Create file: `src/Kakeibo.App/stores/{name}.ts`
2. Use setup function style: `defineStore('name', () => { ... })`
3. Export as `use{Name}Store`
4. Use `ref()` for state, `computed()` for getters, functions for actions

### Adding a NuGet Package

1. Add version to `Directory.Packages.props`: `<PackageVersion Include="..." Version="..." />`
2. Add reference in `src/Kakeibo.Api/Kakeibo.Api.csproj` WITHOUT version: `<PackageReference Include="..." />`
3. Run: `bun run api:restore && bun run api:build`

### Before Committing

1. Backend: `bun run api:format && bun run api:build && bun run api:test`
2. Frontend (when exists): `bun run app:format && bun run app:lint && bun run app:test:unit`
3. Email (if changed): `bun run email:format`
4. Re-stage any files modified by the formatters before committing: `git add <files>`
5. Documentation: Update the corresponding `.claude/` documentation file if the change introduces or modifies a pattern, flow, or architectural concept.
6. README.md: Update the phase status, project structure, and any new commands or service URLs if the change marks a phase milestone.
7. Commit with conventional format: `type(scope): description`
8. Pre-commit hooks will run automatically (commitlint + oxlint + oxfmt on staged files)

---

## Key Patterns (Reference Implementations)

Read these files when implementing similar functionality:

| Pattern | File | What it demonstrates |
|---------|------|---------------------|
| IEndpoint pattern | `src/Kakeibo.Api/Common/Endpoints/IEndpoint.cs` | Minimal API REPR pattern interface |
| Validation filter | `src/Kakeibo.Api/Common/Endpoints/ValidationFilter.cs` | Generic FluentValidation endpoint filter |
| Endpoint scanning | `src/Kakeibo.Api/Common/Endpoints/EndpointExtensions.cs` | Assembly scanning for IEndpoint |
| Password hashing | `src/Kakeibo.Api/Common/Utils/PasswordHasher.cs` | PBKDF2-SHA512, salt generation, constant-time verify |
| JSON serialization | `src/Kakeibo.Api/Common/Utils/DefaultSerializer.cs` | camelCase, null handling |
| ID generation | `src/Kakeibo.Api/Common/Utils/Guid7.cs` | UUIDv7 type-safe wrapper |
| In-process events | `src/Kakeibo.Api/Infrastructure/Events/` | IEventBus, ChannelEventBus, EventDispatcher |
| Pinia store | `src/Kakeibo.App/stores/counter.ts` | Setup function style, ref + computed + functions (when created) |
| String constants | `src/Kakeibo.Api/Common/Utils/CharSets.cs` | Static class with `public const string` fields |

