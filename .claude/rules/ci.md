# CI/CD & Quality Gates

The Kakeibo monorepo uses a fully automated CI/CD pipeline built on GitHub Actions,
semantic-release, and conventional commits. Together, these components eliminate manual
versioning, enforce quality on every contribution, and document every release automatically.

---

## Components

### Commitlint (`commitlint.config.ts`)

Enforces `type(scope): description` format on every commit via the `commit-msg` lefthook.
Valid scopes: `app`, `api`, `android`, `email`, `docs`, `infra`, `deps`, `release`, `skills`, `roadmap`.
Max subject: 100 chars. This is the upstream source for version calculation, CHANGELOG content,
and GitHub Release notes — a poorly worded commit becomes a poorly worded release note.
**Every new project must add its scope here, in `quality.yml`, `quality-check.ts`, and `package.json`.**

### Quality Workflow (`.github/workflows/quality.yml`)

Runs on every pull request to `main`. Four jobs:

| Job | Status | What it validates |
|-----|--------|------------------|
| `quality-api` | ✅ Active | .NET build + format check (`--verify-no-changes`) + unit tests |
| `quality-app` | ✅ Active | Lint + unit tests + build |
| `quality-email` | ✅ Active | Typecheck + lint + format + tests |
| `quality-docker` | ⏸️ Disabled | Docker build validation (no push) |

No code reaches `main` without all active jobs passing. `quality-docker` is disabled until
services are production-ready (change `if: false` to `if: true` to enable).

### `scripts/quality-check.ts`

Local mirror of the CI quality gates. Runs checks sequentially (never parallel) and
skips Testcontainers tests gracefully when Docker is unavailable — avoiding false failures
on machines without Docker. Invoked via `bun run check:api|app|email|docs`.


### Release Workflow (`.github/workflows/release.yml`)

Runs on every push to `main` (after PR merge). The `semantic-release` job always runs;
Docker build jobs are double-gated with `&& false` until services are production-ready.
Change to `&& true` per service when the Dockerfile is production-ready.

### Semantic Release + `.releaserc.json`

Analyzes commits since the last Git tag and runs seven plugins in sequence:

| Plugin | What it does |
|--------|-------------|
| `commit-analyzer` | `feat` → minor, `fix/perf/revert/refactor` → patch, `BREAKING CHANGE:` → major, others → no release |
| `release-notes-generator` | Groups visible commits by type for GitHub Release notes |
| `changelog` | Appends new section to `CHANGELOG.md` |
| `exec` | Writes version to `.version` file for downstream CI use |
| `npm` | Bumps `package.json` version (`npmPublish: false`) |
| `git` | Commits `CHANGELOG.md` + `package.json` back to `main` with `[skip ci]` |
| `github` | Creates the GitHub Release with auto-generated notes |

Only `main` branch triggers releases. The `[skip ci]` suffix on the release commit
prevents the release workflow from re-triggering itself.

### `CHANGELOG.md`

Owned exclusively by semantic-release. Never edit manually — the next release overwrites it.
Content is determined entirely by commit messages since the last tag.

### `package.json` (root)

Defines all monorepo-wide scripts (`api:*`, `app:*`, `email:*`). CI reads these directly.
When a new project is added, its scripts must be registered here simultaneously with
`quality.yml`, `quality-check.ts`, and `commitlint.config.ts` (mandatory.md Rule 6).

---

## Pipeline Flow

```
commit-msg hook validates format via commitlint
    ↓
PR opened → quality.yml runs all active jobs in parallel
    ↓
PR merged to main → release.yml triggers
    ↓
semantic-release analyzes commits since last Git tag
    ↓
Version bump determined → package.json + CHANGELOG.md updated
    ↓
Commit + Git tag pushed → GitHub Release created → [skip ci]
    ↓
Docker build jobs fire (only when && true and release published)
```

---

## What This Prevents

- **Version drift**: No manual `package.json` edits or forgotten tags
- **Undocumented releases**: Every release is reflected in `CHANGELOG.md` automatically
- **Broken code on `main`**: Quality gates block merges that fail build, lint, or tests
- **Docker CI breakage**: Skip guards prevent Testcontainers failures when Docker is absent
- **Scope pollution**: Commitlint rejects commits with unregistered scopes, forcing
  developers to register new projects in all four quality gate locations
