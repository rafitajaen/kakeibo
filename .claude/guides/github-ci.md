# GitHub CI/CD Reference Guide

Complete reference for the CI/CD system: conventional commits, commit hooks, quality gates,
semantic versioning, and GitHub Actions workflows.

---

## Table of Contents

1. [Pipeline Overview](#1-pipeline-overview)
2. [Package Manager (Bun)](#2-package-manager-bun)
3. [Conventional Commits](#3-conventional-commits)
4. [Commitlint](#4-commitlint)
5. [Lefthook (Pre-commit Hooks)](#5-lefthook-pre-commit-hooks)
6. [Root package.json](#6-root-packagejson)
7. [Quality Check Script](#7-quality-check-script)
8. [GitHub Actions — quality.yml](#8-github-actions--qualityyml)
9. [Semantic Release](#9-semantic-release)
10. [GitHub Actions — release.yml](#10-github-actions--releaseyml)
11. [Adding a New Project](#11-adding-a-new-project)
12. [Version Bump Reference](#12-version-bump-reference)
13. [GitHub Secrets Reference](#13-github-secrets-reference)
14. [Troubleshooting](#14-troubleshooting)

---

## 1. Pipeline Overview

```
Developer writes code
        │
        ▼
git commit
        │
        ├─► commit-msg hook (lefthook)
        │       commitlint validates format
        │       → rejected if not conventional
        │
        ├─► pre-commit hook (lefthook)
        │       linters + formatters on staged files
        │       → rejected if errors found
        │
        ▼
git push → Pull Request opened
        │
        ▼
GitHub Actions: quality.yml (runs on every PR)
        │
        ├─► quality-api job    (build + test + format check)
        ├─► quality-app job    (lint + unit tests + typecheck)
        ├─► quality-email job  (lint + format + typecheck)
        └─► quality-docker job (Dockerfile build validation)
        │
        ▼
PR approved + merged to main
        │
        ▼
GitHub Actions: release.yml (runs on push to main)
        │
        ├─► semantic-release job
        │       Analyzes commits since last tag
        │       Determines next version (semver)
        │       Updates package.json version field
        │       Generates / updates CHANGELOG.md
        │       Creates Git tag (vX.Y.Z)
        │       Creates GitHub Release
        │       Commits release changes [skip ci]
        │
        └─► build-push-* jobs (disabled by default — see §10)
                Build Docker images
                Push to container registry
```

**Key principle:** Quality gates run on every PR. Release automation runs only after merge to main.
These two concerns are intentionally separated.

---

## 2. Package Manager (Bun)

**Bun is the only supported package manager.** Never use `npm` or `npx`.

| Situation | Command |
|-----------|---------|
| Install dependencies | `bun install` |
| Install with frozen lockfile (CI) | `bun install --frozen-lockfile` |
| Run a script | `bun run <script>` |
| One-off CLI invocation | `bunx <tool>` |
| CLI tool that requires Bun runtime | `bunx --bun <tool>` |

**`bunx` vs `bunx --bun`:**
- `bunx <tool>` — runs the tool with its own runtime (Node-compatible default). Use for most tools: `bunx commitlint`, `bunx lefthook`, `bunx semantic-release`.
- `bunx --bun <tool>` — forces the tool to run under the Bun runtime. Required for tools that must use Bun's APIs: `bunx --bun shadcn-vue@latest add button`.

**Never use `npx`** in any script, documentation, commit message, or instruction. Replace all `npx` references with the equivalent `bunx` command.

---

## 3. Conventional Commits

All commits must follow this format:

```
type(scope): description

[optional body]

[optional footer]
```

### Types

| Type | Triggers release | Version bump |
|------|-----------------|--------------|
| `feat` | Yes | Minor (0.x.0) |
| `fix` | Yes | Patch (0.0.x) |
| `perf` | Yes | Patch |
| `revert` | Yes | Patch |
| `refactor` | Yes | Patch |
| `docs` | No | — |
| `style` | No | — |
| `chore` | No | — |
| `test` | No | — |
| `build` | No | — |
| `ci` | No | — |

### Breaking changes

Append `!` after `type(scope)` and include `BREAKING CHANGE:` in the footer:

```
feat(api)!: remove deprecated endpoint

BREAKING CHANGE: The /v1/users endpoint has been removed.
Clients must migrate to /v2/users.
```

Both the `!` and the `BREAKING CHANGE:` footer are required for major version bumps.

### Rules

- **Max subject length:** 100 characters
- **Language:** English
- **Scope:** Optional but recommended. Must be in the `scope-enum` list in `commitlint.config.ts`.
- **Description:** Imperative mood ("add endpoint", not "added endpoint" or "adds endpoint")

### Examples

```bash
feat(wallets): add wallet creation endpoint
fix(auth): handle expired token refresh correctly
docs(readme): update service URLs for phase 2
test(transactions): add integration tests for recording flow
ci(release): enable docker build jobs
chore(deps): update bun to 1.2.0
refactor(budgets): extract spending calculation to separate method
```

---

## 4. Commitlint

Commitlint enforces the conventional commits format via the `commit-msg` lefthook.

### Configuration file

**`commitlint.config.ts`** at the repository root:

```typescript
import type { UserConfig } from '@commitlint/types';

const config: UserConfig = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'type-enum': [
      2,
      'always',
      [
        'feat', 'fix', 'docs', 'style', 'refactor',
        'perf', 'test', 'build', 'ci', 'chore', 'revert',
      ],
    ],
    'scope-enum': [
      2,
      'always',
      [
        // List every project/domain in the monorepo
        'api', 'app', 'email', 'docs', 'infra',
        'deps', 'release', 'skills', 'roadmap',
        // Add new scopes here when adding new projects
      ],
    ],
    'subject-max-length': [2, 'always', 100],
    'scope-empty': [1, 'never'],  // Warning (not error) if scope omitted
  },
};

export default config;
```

### Severity levels

| Level | Meaning |
|-------|---------|
| `0` | Disabled |
| `1` | Warning (commit allowed but warned) |
| `2` | Error (commit rejected) |

### Adding a new scope

When adding a new project or domain to the monorepo:

1. Add the scope to `scope-enum` in `commitlint.config.ts`
2. Update the scope list in `CLAUDE.md` (or equivalent project instructions)

**Violation:** Commits with an unregistered scope will be rejected by the `commit-msg` hook.

---

## 5. Lefthook (Pre-commit Hooks)

Lefthook runs git hooks defined in `lefthook.yml` at the repository root.

### Hook: `commit-msg`

Validates the commit message format using commitlint.

```yaml
# lefthook.yml
commit-msg:
  commands:
    commitlint:
      run: bunx commitlint --edit {1}
```

Runs on every `git commit`. The `{1}` placeholder is the path to the commit message file provided by git.

### Hook: `pre-commit`

Runs linters and format checks on staged files before the commit is finalized.

```yaml
# lefthook.yml
pre-commit:
  parallel: true
  commands:
    oxlint:
      glob: "*.{ts,tsx,vue,js,jsx}"
      run: cd src/MyApp && bunx oxlint --deny-warnings {staged_files}
    oxfmt-check:
      glob: "*.{ts,tsx,vue,js,jsx,css,json}"
      run: cd src/MyApp && bunx oxfmt --check {staged_files}
```

Both commands run in parallel. Either failure rejects the commit.

### Developer workflow for commits

The pre-commit hook uses **check mode only** — it never auto-fixes files. Follow this sequence before every commit:

1. Make your changes
2. Run auto-fix for every project you touched:
   ```bash
   bun run app:format && bun run app:lint
   bun run email:format
   # (backend formatting is handled manually by the developer)
   ```
3. **Re-stage any files modified by the formatters:**
   ```bash
   git add <modified-files>
   ```
4. Commit — the pre-commit hook will now pass

**Why re-stage?** Formatters modify files on disk. Git only includes the version of each file
that was staged at commit time. If you forget to re-stage, the pre-commit hook sees the
pre-fix staged version and rejects the commit even though you "already ran the formatter".

### Installing hooks

After cloning the repository:

```bash
bun install          # installs lefthook as a devDependency
bunx lefthook install  # registers the git hooks in .git/hooks/
```

---

## 6. Root package.json

The root `package.json` is the **single entry point for all monorepo scripts**.

### Script naming convention

Scripts follow the pattern `{project}:{operation}`:

```json
{
  "scripts": {
    "api:build":        "dotnet build MyProject.slnx",
    "api:test":         "dotnet test tests/MyProject.Tests/ --configuration Release",
    "api:restore":      "dotnet restore MyProject.slnx",
    "api:run":          "cd src/MyProject.Api && dotnet run",

    "app:dev":          "cd src/MyProject.App && bun run dev",
    "app:build":        "cd src/MyProject.App && bun run build",
    "app:lint":         "cd src/MyProject.App && bunx oxlint .",
    "app:lint:check":   "cd src/MyProject.App && bunx oxlint --deny-warnings .",
    "app:format":       "cd src/MyProject.App && bunx oxfmt .",
    "app:format:check": "cd src/MyProject.App && bunx oxfmt --check .",
    "app:test:unit":    "cd src/MyProject.App && bun run vitest",
    "app:test:e2e":     "cd src/MyProject.App && bunx playwright test",
    "app:typecheck":    "cd src/MyProject.App && bun run vue-tsc --noEmit",

    "email:format":       "cd src/MyProject.Email && bunx oxfmt .",
    "email:format:check": "cd src/MyProject.Email && bunx oxfmt --check .",
    "email:lint":         "cd src/MyProject.Email && bunx oxlint .",
    "email:typecheck":    "cd src/MyProject.Email && bun run tsc --noEmit",

    "docker:up":   "docker compose --profile app up -d",
    "docker:down": "docker compose down",
    "docker:logs": "docker compose logs -f"
  }
}
```

### Key rules

- **Never manually edit `version`** — semantic-release owns it.
- **`devDependencies`** must include: `commitlint`, `lefthook`, `semantic-release` and its plugins, `oxlint`, `oxfmt`.
- Scripts in `package.json` must stay in sync with `scripts/quality-check.ts` — both should reference the same commands and targets.

### devDependencies for CI tooling

```json
{
  "devDependencies": {
    "@commitlint/cli": "^19.x",
    "@commitlint/config-conventional": "^19.x",
    "@commitlint/types": "^19.x",
    "lefthook": "^1.x",
    "semantic-release": "^24.x",
    "@semantic-release/changelog": "^6.x",
    "@semantic-release/commit-analyzer": "^13.x",
    "@semantic-release/exec": "^6.x",
    "@semantic-release/git": "^10.x",
    "@semantic-release/github": "^11.x",
    "@semantic-release/npm": "^12.x",
    "@semantic-release/release-notes-generator": "^14.x"
  }
}
```

---

## 7. Quality Check Script

**`scripts/quality-check.ts`** provides a unified local runner for all quality gates.
It mirrors what the CI pipeline runs, allowing developers to catch issues before pushing.

### Structure

```typescript
// scripts/quality-check.ts

interface Check {
  name: string;
  project: string;
  cmd: string[];
  prerequisite?: () => Promise<boolean>;
  skipReason?: string;
}

const CHECKS: Check[] = [
  // Backend
  { name: "build",        project: "api",   cmd: ["dotnet", "build", "MyProject.slnx"] },
  { name: "test:unit",    project: "api",   cmd: ["dotnet", "test", "tests/MyProject.Tests/", "--configuration", "Release"] },

  // Frontend
  { name: "lint",         project: "app",   cmd: ["bun", "run", "app:lint:check"] },
  { name: "typecheck",    project: "app",   cmd: ["bun", "run", "app:typecheck"] },
  { name: "test:unit",    project: "app",   cmd: ["bun", "run", "app:test:unit"] },

  // Email
  { name: "lint",         project: "email", cmd: ["bun", "run", "email:lint"] },
  { name: "typecheck",    project: "email", cmd: ["bun", "run", "email:typecheck"] },

  // Docker (optional, skipped if Docker unavailable)
  {
    name: "docker:build",
    project: "infra",
    cmd: ["docker", "build", "-f", "src/MyProject.Api/Dockerfile", "."],
    prerequisite: hasDocker,
    skipReason: "Docker not available",
  },
];
```

### Sequential execution

**CRITICAL:** Always run checks **sequentially**, never in parallel subagents or background processes.
Running build + test + format simultaneously can saturate the host OS and produce unreliable results.

```typescript
// Correct: sequential
for (const check of CHECKS) {
  await runCheck(check);
}

// WRONG: parallel (never do this)
await Promise.all(CHECKS.map(check => runCheck(check)));
```

### Docker skip pattern

Checks that require Docker must use a `prerequisite` function so they are skipped gracefully
in CI environments without Docker access:

```typescript
async function hasDocker(): Promise<boolean> {
  try {
    const proc = Bun.spawn(["docker", "info"], { stdout: "ignore", stderr: "ignore" });
    const code = await proc.exited;
    return code === 0;
  } catch {
    return false;
  }
}
```

### Running it

```bash
bun run scripts/quality-check.ts
# or via package.json script:
bun run check
```

---

## 8. GitHub Actions — quality.yml

The quality workflow runs on every pull request targeting `main`.

### Trigger

```yaml
on:
  pull_request:
    branches:
      - main
```

This prevents duplicate runs — quality gates run only on PRs, not on direct pushes.

### Job structure

One job per project. Each job is independently toggleable with `if: true/false`.

```yaml
jobs:
  quality-api:
    name: Quality - API
    runs-on: ubuntu-latest
    if: true   # set to false to disable this job
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.x'
      - uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/Directory.Packages.props') }}
          restore-keys: |
            ${{ runner.os }}-nuget-
      - run: dotnet restore MyProject.slnx
      - run: dotnet build MyProject.slnx --no-restore --configuration Release
      - run: dotnet format MyProject.slnx --verify-no-changes
      - run: dotnet test tests/MyProject.Tests/ --no-build --configuration Release

  quality-app:
    name: Quality - App
    runs-on: ubuntu-latest
    if: true   # set to false when app doesn't exist yet
    steps:
      - uses: actions/checkout@v4
      - uses: oven-sh/setup-bun@v2
      - uses: actions/cache@v4
        with:
          path: ~/.bun/install/cache
          key: ${{ runner.os }}-bun-${{ hashFiles('bun.lock') }}
          restore-keys: |
            ${{ runner.os }}-bun-
      - run: bun install --frozen-lockfile
      - run: bun run app:lint:check
      - run: bun run app:typecheck
      - run: bun run app:test:unit

  quality-email:
    name: Quality - Email
    runs-on: ubuntu-latest
    if: true
    steps:
      - uses: actions/checkout@v4
      - uses: oven-sh/setup-bun@v2
      - run: bun install --frozen-lockfile
      - run: bun run email:typecheck
      - run: bun run email:lint
      - run: bun run email:format:check

  quality-docker:
    name: Quality - Docker
    runs-on: ubuntu-latest
    if: true   # disable until Dockerfiles are production-ready
    steps:
      - uses: actions/checkout@v4
      - run: docker build -f src/MyProject.Api/Dockerfile . --no-cache
      - run: docker build -f src/MyProject.App/Dockerfile src/MyProject.App --no-cache
      - run: docker build -f src/MyProject.Email/Dockerfile src/MyProject.Email --no-cache
```

### Enabling / disabling jobs

Set `if: true` to enable, `if: false` to disable. A disabled job is skipped entirely — it does not block the PR.

**Common pattern:** Disable `quality-app` until the frontend project is created. Disable `quality-docker` until Dockerfiles are production-ready.

### Caching strategy

Cache per project type:

| Project type | Cache path | Cache key |
|-------------|-----------|-----------|
| .NET (NuGet) | `~/.nuget/packages` | Hash of `Directory.Packages.props` |
| Bun | `~/.bun/install/cache` | Hash of `bun.lock` |

---

## 9. Semantic Release

Semantic-release automates the entire release process: version calculation, changelog generation,
GitHub Release creation, and tag pushing.

### How it works

```
Push to main
    ↓
Analyze all commits since the last tag
    ↓
Determine next version based on commit types
    ↓
Update package.json version field
    ↓
Append entry to CHANGELOG.md
    ↓
Commit: "chore(release): X.Y.Z [skip ci]"
    ↓
Create Git tag: vX.Y.Z
    ↓
Push commit + tag
    ↓
Create GitHub Release with auto-generated notes
```

The `[skip ci]` suffix on the release commit prevents infinite workflow loops.

### Initial setup (required before first release)

> **CRITICAL: Create the `v0.0.0` tag before the first merge to `main`.**
>
> Semantic-release needs an existing tag to calculate the **next** version.
> Without a starting tag, it has no reference point and will assign `v1.0.0`
> instead of the expected `v0.1.0`.
>
> **Do this once, immediately after repository creation:**
> ```bash
> git checkout main
> git tag -a v0.0.0 -m "chore(release): initial tag for semantic-release"
> git push origin v0.0.0
> ```
>
> The tag must be **annotated** (`-a` flag). Semantic-release ignores lightweight tags.
> Verify it appeared under Repository → Releases → Tags on GitHub.

### GitHub Personal Access Token (required)

The default `GITHUB_TOKEN` provided by GitHub Actions cannot create releases or push tags.
A Personal Access Token (PAT) is required.

**Create the PAT:**

1. GitHub.com → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Name: `project-semantic-release`
3. Expiration: No expiration (or 1 year minimum)
4. Scopes: ✅ `repo` (full) + ✅ `workflow`
5. Generate and copy immediately (shown only once)

**Add as a repository secret:**

1. Repository → Settings → Secrets and variables → Actions → New repository secret
2. Name: `GH_TOKEN`
3. Value: the PAT copied above

### Configuration — `.releaserc.json`

```json
{
  "branches": ["main"],
  "repositoryUrl": "https://github.com/your-org/your-repo.git",
  "plugins": [
    [
      "@semantic-release/commit-analyzer",
      {
        "preset": "conventionalcommits",
        "releaseRules": [
          { "type": "feat",     "release": "minor" },
          { "type": "fix",      "release": "patch" },
          { "type": "perf",     "release": "patch" },
          { "type": "revert",   "release": "patch" },
          { "type": "refactor", "release": "patch" },
          { "type": "docs",     "release": false   },
          { "type": "style",    "release": false   },
          { "type": "chore",    "release": false   },
          { "type": "test",     "release": false   },
          { "type": "build",    "release": false   },
          { "type": "ci",       "release": false   },
          { "breaking": true,   "release": "major" }
        ]
      }
    ],
    [
      "@semantic-release/release-notes-generator",
      {
        "preset": "conventionalcommits",
        "presetConfig": {
          "types": [
            { "type": "feat",     "section": "Features",         "hidden": false },
            { "type": "fix",      "section": "Bug Fixes",        "hidden": false },
            { "type": "perf",     "section": "Performance",      "hidden": false },
            { "type": "revert",   "section": "Reverts",          "hidden": false },
            { "type": "refactor", "section": "Code Refactoring", "hidden": false },
            { "type": "docs",     "section": "Documentation",    "hidden": true  },
            { "type": "style",    "section": "Styles",           "hidden": true  },
            { "type": "chore",    "section": "Chores",           "hidden": true  },
            { "type": "test",     "section": "Tests",            "hidden": true  },
            { "type": "build",    "section": "Build System",     "hidden": true  },
            { "type": "ci",       "section": "CI",               "hidden": true  }
          ]
        }
      }
    ],
    ["@semantic-release/changelog", { "changelogFile": "CHANGELOG.md" }],
    ["@semantic-release/npm",       { "npmPublish": false }],
    [
      "@semantic-release/git",
      {
        "assets": ["CHANGELOG.md", "package.json"],
        "message": "chore(release): ${nextRelease.version} [skip ci]\n\n${nextRelease.notes}"
      }
    ],
    "@semantic-release/github"
  ]
}
```

### devDependencies — required or optional?

The semantic-release packages in `devDependencies` are **not strictly required**. Both approaches work:

**Without devDependencies (other projects may use this):**
```yaml
# CI — no bun install needed for semantic-release
- run: bunx semantic-release
```
`bunx` downloads `semantic-release` on the fly. Since v19+, semantic-release also
**auto-installs its plugins** declared in `.releaserc.json` at runtime if they are not
already in `node_modules`.

**With devDependencies (what this project does):**
```yaml
# CI
- run: bun install --frozen-lockfile
- run: bunx semantic-release
```
Plugins are already in `node_modules` — no runtime downloads needed.

| Aspect | Without devDeps | With devDeps |
|--------|----------------|--------------|
| CI speed | Slower (downloads plugins each run) | Faster (already installed) |
| Versions locked | No (uses latest at run time) | Yes (bun.lock controls versions) |
| `package.json` cleanliness | Cleaner | More entries |
| Risk of breakage by plugin update | Higher | Lower |
| `bun install` required in CI | No | Yes |

Since this project already runs `bun install --frozen-lockfile` in CI (for commitlint,
lefthook, and other tooling), keeping semantic-release in `devDependencies` adds no extra
cost and provides reproducible, version-locked builds.

### What semantic-release manages (never edit manually)

- `package.json` → `version` field
- `CHANGELOG.md` — overwriting or editing will cause conflicts
- Git tags (`vX.Y.Z`)
- GitHub Releases

---

## 10. GitHub Actions — release.yml

The release workflow runs on every push to `main` (i.e., after every PR merge).

### Trigger

```yaml
on:
  push:
    branches:
      - main
```

### Job: semantic-release

```yaml
jobs:
  semantic-release:
    name: Semantic Release
    runs-on: ubuntu-latest
    if: true   # set to false to disable all releases
    outputs:
      new_release_published: ${{ steps.semantic.outputs.new_release_published }}
      new_release_version:   ${{ steps.semantic.outputs.new_release_version }}
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0                         # full history required for commit analysis
          token: ${{ secrets.GH_TOKEN }}         # PAT, not GITHUB_TOKEN
      - uses: oven-sh/setup-bun@v2
      - run: bun install --frozen-lockfile
      - id: semantic
        env:
          GITHUB_TOKEN: ${{ secrets.GH_TOKEN }}
          GIT_AUTHOR_NAME:      github-actions[bot]
          GIT_AUTHOR_EMAIL:     github-actions[bot]@users.noreply.github.com
          GIT_COMMITTER_NAME:   github-actions[bot]
          GIT_COMMITTER_EMAIL:  github-actions[bot]@users.noreply.github.com
        run: bunx semantic-release
```

### Jobs: build-push-* (disabled by default)

Docker image builds are gated by two conditions:

```yaml
if: needs.semantic-release.outputs.new_release_published == 'true' && false
#                                                                      ^^^^^
#                                                            remove to enable
```

The `&& false` keeps Docker builds disabled even when semantic-release creates a release.
Remove `&& false` (change to `&& true`) for each service when it is production-ready.

```yaml
  build-push-api:
    name: Build & Push API
    runs-on: ubuntu-latest
    needs: semantic-release
    if: needs.semantic-release.outputs.new_release_published == 'true' && false
    steps:
      - uses: actions/checkout@v4
      - uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKER_HUB_USERNAME }}
          password: ${{ secrets.DOCKER_HUB_TOKEN }}
      - uses: docker/build-push-action@v6
        with:
          context: .
          file: src/MyProject.Api/Dockerfile
          push: true
          tags: |
            ${{ secrets.DOCKER_HUB_USERNAME }}/myproject-api:latest
            ${{ secrets.DOCKER_HUB_USERNAME }}/myproject-api:v${{ needs.semantic-release.outputs.new_release_version }}
            ${{ secrets.DOCKER_HUB_USERNAME }}/myproject-api:sha-${{ github.sha }}
```

### Enabling Docker builds

When a service is ready for production Docker builds:

1. Change its job's `if` condition:
   ```yaml
   # Before:
   if: needs.semantic-release.outputs.new_release_published == 'true' && false
   # After:
   if: needs.semantic-release.outputs.new_release_published == 'true' && true
   ```
2. Ensure `DOCKER_HUB_USERNAME` and `DOCKER_HUB_TOKEN` secrets are configured (see §13).

---

## 11. Adding a New Project

When adding any new project to the monorepo, register it in all four locations simultaneously.
Missing any location leaves the project unprotected by CI.

### Location 1 — `commitlint.config.ts`

Add the new scope to `scope-enum`:

```typescript
'scope-enum': [2, 'always', [
  'api', 'app', 'email', ...,
  'new-project',   // ← add here
]],
```

Also update the scope list in your project's equivalent of `CLAUDE.md`.

### Location 2 — `package.json`

Add scripts for the new project's core operations:

```json
{
  "scripts": {
    "new-project:build":   "cd src/NewProject && bun run build",
    "new-project:lint":    "cd src/NewProject && bunx oxlint .",
    "new-project:test":    "cd src/NewProject && bun run test",
    "new-project:format":  "cd src/NewProject && bunx oxfmt ."
  }
}
```

### Location 3 — `scripts/quality-check.ts`

Add entries for the new project in the `CHECKS` array:

```typescript
{ name: "build",    project: "new-project", cmd: ["bun", "run", "new-project:build"] },
{ name: "lint",     project: "new-project", cmd: ["bun", "run", "new-project:lint"] },
{ name: "test",     project: "new-project", cmd: ["bun", "run", "new-project:test"] },
```

For projects requiring Docker (e.g., Testcontainers integration tests):

```typescript
{
  name: "test:integration",
  project: "new-project",
  cmd: ["bun", "run", "new-project:test:integration"],
  prerequisite: hasDocker,
  skipReason: "Docker not available",
}
```

### Location 4 — `.github/workflows/quality.yml`

Add a dedicated job for the new project:

```yaml
  quality-new-project:
    name: Quality - New Project
    runs-on: ubuntu-latest
    if: true
    steps:
      - uses: actions/checkout@v4
      - uses: oven-sh/setup-bun@v2
      - run: bun install --frozen-lockfile
      - run: bun run new-project:lint
      - run: bun run new-project:test
```

---

## 12. Version Bump Reference

| Commit type(s) since last tag | Resulting bump | Example |
|------------------------------|----------------|---------|
| `feat` only | Minor: 0.x.0 | `v0.1.0` → `v0.2.0` |
| `fix` / `perf` / `revert` / `refactor` | Patch: 0.0.x | `v0.2.0` → `v0.2.1` |
| `feat` + `fix` (mixed) | Minor (highest wins) | `v0.2.1` → `v0.3.0` |
| Any with `BREAKING CHANGE:` | Major: x.0.0 | `v0.2.1` → `v1.0.0` |
| `docs` / `style` / `chore` / `test` / `build` / `ci` only | No release | — |
| No commits since last tag | No release | — |

**Rule:** When multiple commit types are present, the highest-priority type determines the bump.
Priority order: BREAKING > feat > fix/perf/revert/refactor.

---

## 13. GitHub Secrets Reference

Configure under: Repository → Settings → Secrets and variables → Actions → Repository secrets.

### Required for semantic-release

| Secret | Description | When needed |
|--------|-------------|-------------|
| `GH_TOKEN` | Personal Access Token with `repo` + `workflow` scopes | Always — required for releases |

### Required for Docker builds

| Secret | Description | When needed |
|--------|-------------|-------------|
| `DOCKER_HUB_USERNAME` | Docker Hub account username | When `build-push-*` jobs are enabled |
| `DOCKER_HUB_TOKEN` | Docker Hub access token (not password) | When `build-push-*` jobs are enabled |

**Creating a Docker Hub access token:**
Docker Hub → Account Settings → Security → Access Tokens → New Access Token.
Use Read & Write permission.

### Application secrets (project-specific)

These are injected into containers at deploy time. Examples:

| Secret | Description |
|--------|-------------|
| `POSTGRES_PASSWORD` | Database password |
| `REDIS_PASSWORD` | Cache password |
| `JWT_SECRET_KEY` | Token signing key (min 32 chars) |
| `STORAGE_SECRET_KEY` | Object storage secret key |

Application secrets are never stored in the repository. They are injected as environment
variables by Docker Compose at runtime, sourced from the server's `.env` file which is
generated from GitHub Secrets during deployment.

---

## 14. Troubleshooting

### Commit rejected: "type may not be empty"

**Cause:** Commit message does not follow the `type(scope): description` format.

**Fix:** Rewrite the commit message:
```bash
git commit --amend -m "feat(api): add user registration endpoint"
```

### Commit rejected: "scope must be one of [...]"

**Cause:** The scope used is not in the `scope-enum` list in `commitlint.config.ts`.

**Fix:** Either use an existing scope, or add the new scope to `commitlint.config.ts` first.

### Pre-commit hook rejected: oxfmt-check failed

**Cause:** Staged files are not properly formatted.

**Fix:**
```bash
bun run app:format     # auto-fix formatting
git add <fixed-files>  # re-stage the fixed files
git commit             # retry
```

### Semantic-release creates v1.0.0 instead of v0.1.0

**Cause:** The `v0.0.0` initial tag was not created before the first merge to `main`.
Semantic-release found no existing tag and treated all commits as the first release.

**Fix (if it has already happened):**
- You cannot easily roll back. Accept `v1.0.0` as the new baseline and continue from there.

**Prevention:** Always create `v0.0.0` before merging any branch to `main`.

### Semantic-release fails: "The operation was canceled" or "ENOENT"

**Cause:** Missing `GH_TOKEN` secret, or token lacks required permissions.

**Fix:**
1. Verify `GH_TOKEN` exists under Repository → Secrets and variables → Actions
2. Verify the PAT has `repo` + `workflow` scopes
3. If the token has expired, generate a new one and update the secret

### Release created but no GitHub Release visible

**Cause:** The `@semantic-release/github` plugin failed silently, or the PAT lacks
`public_repo` scope.

**Fix:** Check the Actions run log for the `semantic-release` job for errors from the
`@semantic-release/github` plugin step.

### Docker build job not running after release

**Expected behavior:** Docker jobs are intentionally disabled with `&& false`.

**To enable:** Remove `&& false` from the job's `if` condition in `release.yml`.

### "A tag already exists for version X.Y.Z"

**Cause:** Tag was created manually before semantic-release ran, or a previous run partially succeeded.

**Fix:**
```bash
git tag -d vX.Y.Z                          # delete local tag
git push origin :refs/tags/vX.Y.Z          # delete remote tag
```
Then re-run the workflow (push an empty commit or re-trigger manually).

### Quality workflow runs twice (on PR and on push)

**Cause:** The trigger includes both `pull_request` and `push`.

**Fix:** The quality workflow should only trigger on `pull_request`. Remove any `push` trigger
from `quality.yml`. The `release.yml` handles the post-merge push.

---

*This guide is project-agnostic. Replace `MyProject`, `your-org`, and `your-repo` with your
actual project identifiers when applying to a new repository.*
