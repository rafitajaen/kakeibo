# Mandatory Business Rules

Invariants that must be enforced at all times, both in backend handlers and frontend UI.

---

## Rule 1: Admin Role Is Immutable

The **Admin** role cannot be deleted, renamed, or have its permissions modified.

- `DELETE /api/roles/{id}` must return `400 Role.AdminProtected` if the role is Admin.
- `PATCH /api/roles/{id}/name` must return `400 Role.AdminProtected` if the role is Admin.
- `PATCH /api/roles/{id}/permissions` must return `400 Role.AdminProtected` if the role is Admin.
- Frontend must hide delete and edit-permissions buttons for the Admin role.

## Rule 2: System Must Always Have At Least One Admin

When deleting a user or changing their role, the system must verify that the operation does not remove the **last active Admin**.

- `DELETE /api/users/{id}` must return `400 User.LastAdmin` if the user is the last non-deleted Admin.
- `PATCH /api/users/{id}/role` must return `400 User.LastAdmin` if the user is the last non-deleted Admin and the new role differs from the current one.
- Frontend must hide delete and change-role buttons for an Admin user when they are the only active Admin.

## Rule 3: Each Deployable Service Must Own Its Dockerfile

Every deployable application must have a `Dockerfile` and a `.dockerignore`
(or `Dockerfile.dockerignore` when the build context cannot be the project root)
inside its own project directory. Dockerfiles must never be placed at the monorepo root.

| Project | Dockerfile | dockerignore | Build context |
|---------|-----------|--------------|---------------|
| `src/Kakeibo.Api/` | `src/Kakeibo.Api/Dockerfile` | `src/Kakeibo.Api/Dockerfile.dockerignore` | `.` (repo root) |
| `sites/Kakeibo.App/` | `sites/Kakeibo.App/Dockerfile` | `sites/Kakeibo.App/.dockerignore` | `./sites/Kakeibo.App` |
| `services/Kakeibo.Email/` | `services/Kakeibo.Email/Dockerfile` | `services/Kakeibo.Email/.dockerignore` | `./services/Kakeibo.Email` |

The API requires repo-root context because its Dockerfile copies from multiple `src/`
subdirectories. In that case the per-Dockerfile dockerignore is named `Dockerfile.dockerignore`
and placed alongside the Dockerfile (Docker resolves it as `{context}/{dockerfile-path}.dockerignore`).

## Rule 4: Never Use `.WithReuse(true)` in Testcontainers

`.WithReuse(true)` is prohibited in all test files (`tests/`).

When combined with `.Build()` as a static field initializer, `.WithReuse(true)` causes
`PostgreSqlBuilder.Build()` to call `Validate()`, which attempts a Docker connection at
**class load time** (the static constructor). This happens before any `Assert.Skip()` guard
can run, causing tests to **fail** instead of being skipped in CI environments without Docker.

- Every `new PostgreSqlBuilder(...)` call in tests must NOT include `.WithReuse(true)`.
- The skip guard pattern from KB-008 is sufficient for CI compatibility.
- The marginal speed gain of container reuse does not justify the CI breakage risk.

## Rule 5: All User-Visible Text Must Use i18n in Frontend Apps

Every string displayed to users in `sites/Kakeibo.App/` **must** be
translated via vue-i18n's `t()` function. Hardcoded text in any language is prohibited.

- All user-visible strings in `<template>` must use `{{ t('key') }}` or `:prop="t('key')"`.
- All user-visible strings in `<script setup>` (toast messages, confirm dialogs, validation
  messages in Zod schemas) must use `t('key')` from `useI18n()`.
- Translation keys must exist in **both** `locales/en.json` and `locales/es.json` before
  the component is considered complete.
- Dynamic data from the API (names, emails, etc.) is exempt — only hardcoded literals are
  prohibited.
- The `@` character in locale values must be escaped as `{'@'}` (see KB-001).

## Rule 6: Every New Project Must Be Registered in package.json, quality-check.ts, CI, and commitlint

When any new project is added to the monorepo — a backend module, a test project, a frontend
site, or a service — it must be simultaneously registered in all four quality gate locations:

1. **`package.json`** — Scripts `{project}:*` for the project's core operations (build, test,
   lint, format, typecheck…). Examples by project type:

   **Backend test project:**
   ```json
   "api:test:notifications": "dotnet test tests/Kakeibo.Modules.Notifications.Tests/..."
   ```

   **Frontend site or service:**
   ```json
   "app:lint": "cd sites/Kakeibo.App && bunx oxlint .",
   "app:test:unit": "cd sites/Kakeibo.App && bun run vitest"
   ```

2. **`scripts/quality-check.ts`** — Entry in the `CHECKS` array with the correct `project`
   field. Examples by project type:

   **Backend test project (Testcontainers):**
   ```typescript
   { name: "test:notifications", project: "api", cmd: ["dotnet", "test", "..."],
     prerequisite: hasDocker, skipReason: "Docker not available" }
   ```

   **Frontend site or service:**
   ```typescript
   { name: "lint", project: "app", cmd: ["bun", "run", "app:lint"] },
   { name: "test:unit", project: "app", cmd: ["bun", "run", "app:test:unit"] }
   ```

3. **`.github/workflows/ci.yml`** — A dedicated job for the project, or a new step inside an
   existing job if the project is a subcomponent. Examples:

   **Backend test project (step inside `quality-api` job):**
   ```yaml
   - name: Test notifications module
     run: dotnet test tests/Kakeibo.Modules.Notifications.Tests/... --no-build --configuration Release
   ```

   **Frontend site or service (dedicated job):**
   ```yaml
   quality-app:
     name: Quality - App
     runs-on: ubuntu-latest
     steps:
       - uses: actions/checkout@v4
       - uses: oven-sh/setup-bun@v2
       - run: bun install --frozen-lockfile
       - run: bun run app:lint
       - run: bun run app:test:unit
   ```

4. **`commitlint.config.ts`** — New scope added to `scope-enum`. Update `CLAUDE.md`
   (section "Git Conventions > Scopes") to keep it in sync:
   ```typescript
   // commitlint.config.ts
   'scope-enum': [2, 'always', ['app', 'api', 'email', 'docs', ..., 'new-project']],
   ```

**Violation:** A project not registered in all four locations is not protected by the CI
pipeline, commits for it may fail commitlint validation, and any regression will go undetected.

## Rule 7: Never Run `dotnet format` — User Runs It Manually

`dotnet format` must **never** be executed by Claude in any form. The user always runs formatting manually from the terminal.

**Prohibited commands (never run these):**
- `dotnet format ...` (any variant, any flags)
- `bun run api:format`
- `bun run api:format:check`
- `bun run api:format:whitespace`
- `bun run api:format:whitespace:check`
- `bun run api:format:style`
- `bun run api:format:style:check`

**Safe quality check commands** (format checks removed from quality-check.ts):
- `bun run check:api` — safe (format checks removed)
- `bun run check:app`, `bun run check:email`, `bun run check:docs` — always safe

The GitHub Actions CI pipeline (`quality-api` job) is the only automated runner that executes
`dotnet format --verify-no-changes`. CI runs it directly, not via quality-check.ts.
This rule does not affect CI.

## Rule 8: Always Use Primary Constructors in C#

All classes and records in `src/` must use C# 12 primary constructors. Traditional constructors with explicit bodies are prohibited.

- `.editorconfig` enforces `csharp_style_prefer_primary_constructors = true:warning`.
- With `TreatWarningsAsErrors` enabled, any traditional constructor triggers a build error.
- Use primary constructors for DI injection, configuration, and all other constructor dependencies.

## Rule 9: Always Use `bunx` Instead of `npx` for CLI Commands

Any CLI command that would normally be invoked with `npx` must use `bunx` instead.
The `--bun` flag must be added when the tool needs to run under the Bun runtime.

- **Never use `npx`** in any script, documentation, commit message, or instruction.
- Use `bunx <tool>` for one-off CLI invocations (e.g., `bunx commitlint`, `bunx lefthook`).
- Use `bunx --bun <tool>` when the tool must run under the Bun runtime (e.g., `bunx --bun shadcn-vue@latest add <component>`). See KB-003 for the shadcn-vue case.
- Run `bunx` from the **project folder** when the tool reads project-local config (e.g., `shadcn-vue`, `oxlint`, `oxfmt`).
- This rule applies to all documentation, code comments, `README.md`, `CLAUDE.md`, and all `.claude/` rule files.

**Examples:**

| Bad (npx) | Good (bunx) |
|-----------|-------------|
| `npx shadcn-vue@latest add button` | `bunx --bun shadcn-vue@latest add button` |
| `npx commitlint --edit` | `bunx commitlint --edit` |
| `npx lefthook install` | `bunx lefthook install` |
| `npx oxlint .` | `bunx oxlint .` |
