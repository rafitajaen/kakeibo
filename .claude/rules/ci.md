# CI/CD & Semantic Release

Complete reference for CI/CD pipelines and automated versioning in the Kakeibo monorepo.

---

## Table of Contents

1. [CI/CD Overview](#cicd-overview)
2. [Semantic Release](#semantic-release)
3. [Initial Setup](#initial-setup)
4. [Workflow Details](#workflow-details)
5. [Configuration Files](#configuration-files)
6. [Release Process](#release-process)
7. [Troubleshooting](#troubleshooting)
8. [Maintenance](#maintenance)

---

## CI/CD Overview

### Workflow Architecture

The repository uses GitHub Actions with two primary workflows:

| Workflow | Trigger | Purpose | Jobs |
|----------|---------|---------|------|
| `quality.yml` | Pull requests to `main` | Quality gates | lint, format, test, build |
| `release.yml` | Push to `main` | Release + Docker builds | semantic-release, build-push-* |

**Separation of concerns:**
- Quality gates run on **every PR** — no code reaches `main` without passing
- Release workflow runs **only after merge** — creates versions and deploys artifacts

### Workflow Status

Both workflows have **conditional jobs** that can be enabled/disabled:

**`quality.yml`:**
- `quality-api`: ✅ Enabled when Identity module has tests
- `quality-app`: ⏸️ Disabled until `sites/Kakeibo.App` exists
- `quality-email`: ✅ Enabled
- `quality-docker`: ⏸️ Disabled until services are production-ready

**`release.yml`:**
- `semantic-release`: ✅ Enabled (creates versions, updates CHANGELOG)
- `build-push-api`: ⏸️ Disabled until API is production-ready
- `build-push-app`: ⏸️ Disabled until App exists
- `build-push-email`: ⏸️ Disabled until Email service is production-ready

**Double-gating pattern:**
```yaml
if: needs.semantic-release.outputs.new_release_published == 'true' && false
```
The `&& false` keeps Docker builds disabled even when semantic-release creates a release. Change to `&& true` when ready to enable automatic Docker image building.

---

## Semantic Release

### What is Semantic Release?

[semantic-release](https://semantic-release.gitbook.io/) automates the entire versioning and release process:
- Analyzes commit messages to determine version bumps
- Updates `package.json` version field
- Generates `CHANGELOG.md` from commit history
- Creates Git tags (`v0.1.0`, `v0.2.0`, etc.)
- Publishes GitHub Releases with auto-generated notes
- Commits changes back to the repository

### Why Use Semantic Release?

**Benefits:**
- **Consistency:** Versions always follow semver based on commit types
- **Automation:** No manual version bumping or CHANGELOG editing
- **Transparency:** Release notes auto-generated from commits
- **Enforced Discipline:** Requires conventional commits (enforced by commitlint)
- **No Human Error:** Can't forget to bump version or tag a release

**Alternative (manual versioning):**
- Developer decides version number (subjective, inconsistent)
- Developer manually edits `package.json`
- Developer manually writes CHANGELOG entry
- Developer manually creates Git tag
- Developer manually creates GitHub Release
- High risk of mistakes and forgotten steps

### How It Works (High-Level)

```
Merge to main
    ↓
GitHub Actions: release.yml runs
    ↓
semantic-release job starts
    ↓
Analyze commits since last tag
    ↓
Determine next version (feat → minor, fix → patch, BREAKING → major)
    ↓
Update package.json (version field)
    ↓
Generate CHANGELOG.md entry
    ↓
Create commit: "chore(release): X.Y.Z [skip ci]"
    ↓
Create Git tag: vX.Y.Z
    ↓
Push commit + tag to repository
    ↓
Create GitHub Release with notes
```

**Important:** The `[skip ci]` suffix prevents infinite loops — the release commit doesn't trigger the workflow again.

---

## Initial Setup

### Prerequisites

1. **Personal Access Token (PAT)** with `repo` + `workflow` scopes
2. **Tag `v0.0.0`** on the initial commit (semantic-release needs a starting point)
3. **Workflow enabled** in `.github/workflows/release.yml` (change `if: false` to `if: true`)

### Step 1: Create GitHub Personal Access Token

**Why not use `GITHUB_TOKEN`?**
The default `GITHUB_TOKEN` provided by GitHub Actions has limited permissions — it **cannot** create releases or push tags. A Personal Access Token (PAT) is required.

**Instructions:**

1. Go to GitHub.com → Settings (your profile) → Developer settings
2. Click "Personal access tokens" → "Tokens (classic)"
3. Click "Generate new token" → "Generate new token (classic)"
4. Configure:
   - **Name:** `kakeibo-semantic-release`
   - **Expiration:** No expiration (or 1 year minimum)
   - **Scopes:**
     - ✅ `repo` (Full control of private repositories) — **REQUIRED**
       - Includes: repo:status, repo_deployment, public_repo, repo:invite, security_events
     - ✅ `workflow` (Update GitHub Action workflows) — **REQUIRED**
5. Click "Generate token"
6. **COPY the token immediately** — it only shows once

### Step 2: Configure Secret in Repository

1. Go to: Repository → Settings → Secrets and variables → Actions
2. Click "New repository secret"
3. Configure:
   - **Name:** `GH_TOKEN`
   - **Secret:** Paste the token copied in Step 1
4. Click "Add secret"
5. Verify: The secret `GH_TOKEN` appears in the list of Repository secrets

**Security note:** Secrets are encrypted at rest and masked in logs. Never print or echo secrets in workflow steps.

### Step 3: Create Initial Tag

**Why `v0.0.0`?**
Semantic-release needs a reference point to calculate the next version. Without a tag, it doesn't know what version to start from.

**Commands:**

```bash
# Switch to main branch
git checkout main

# Create annotated tag on the initial commit
git tag -a v0.0.0 -m "chore(release): initial tag for semantic-release"

# Verify tag was created
git tag -l
# Should show: v0.0.0

# View tag details
git show v0.0.0
# Should show: commit "Initial commit" with tag message

# Push tag to remote
git push origin v0.0.0

# Verify on GitHub
# Go to: Repository → Releases → Tags
# Tag v0.0.0 should appear
```

**Note:** The tag must be **annotated** (`-a` flag), not lightweight. Semantic-release ignores lightweight tags.

### Step 4: Enable Workflow

**File:** `.github/workflows/release.yml`

**Change:**

```yaml
# Line 12 - BEFORE
if: false  # Disabled by default - change to true to activate

# Line 12 - AFTER
if: true  # Enabled - semantic-release active
```

**Commit the change:**

```bash
# In branch init (or any feature branch)
git add .github/workflows/release.yml
git commit -m "ci(release): enable semantic-release workflow"
```

**Note:** The `build-push-*` jobs remain disabled (`&& false`) until services are production-ready. This is intentional.

---

## Workflow Details

### Workflow: `release.yml`

**Trigger:**
```yaml
on:
  push:
    branches:
      - main
```
Runs **only** when commits are pushed directly to `main` (usually after a PR merge).

### Job: `semantic-release`

**Runner:** `ubuntu-latest` (GitHub-hosted)

**Steps:**

1. **Checkout**
   ```yaml
   uses: actions/checkout@v4
   with:
     fetch-depth: 0          # Fetch all history (needed for commit analysis)
     token: ${{ secrets.GH_TOKEN }}  # Use PAT (not default GITHUB_TOKEN)
   ```

2. **Setup Bun**
   ```yaml
   uses: oven-sh/setup-bun@v2
   ```
   Installs Bun runtime (needed for `bunx semantic-release`)

3. **Install dependencies**
   ```yaml
   run: bun install --frozen-lockfile
   ```
   Installs `semantic-release` and its plugins from `package.json`

4. **Run semantic-release**
   ```yaml
   id: semantic
   env:
     GITHUB_TOKEN: ${{ secrets.GH_TOKEN }}
     GIT_AUTHOR_NAME: github-actions[bot]
     GIT_AUTHOR_EMAIL: github-actions[bot]@users.noreply.github.com
     GIT_COMMITTER_NAME: github-actions[bot]
     GIT_COMMITTER_EMAIL: github-actions[bot]@users.noreply.github.com
   run: bunx semantic-release
   ```

**Outputs:**
- `new_release_published`: `true` if a release was created, `false` otherwise
- `new_release_version`: Version number (e.g., `0.2.0`)

These outputs are used by downstream jobs (`build-push-*`) to conditionally run.

### Jobs: `build-push-*` (Disabled by Default)

Three jobs build and push Docker images to Docker Hub:
- `build-push-api`: Builds `src/Kakeibo.Api/Dockerfile` → `<username>/kakeibo-api`
- `build-push-app`: Builds `sites/Kakeibo.App/Dockerfile` → `<username>/kakeibo-app`
- `build-push-email`: Builds `services/Kakeibo.Email/Dockerfile` → `<username>/kakeibo-email`

**Condition:**
```yaml
if: needs.semantic-release.outputs.new_release_published == 'true' && false
```
The `&& false` keeps them disabled. Change to `&& true` when ready.

**Tags applied:**
- `latest` — Always points to newest release
- `vX.Y.Z` — Specific version (e.g., `v0.2.0`)
- `sha-<git-sha>` — Git commit SHA for traceability

---

## Configuration Files

### `.releaserc.json`

**Location:** Repository root

**Purpose:** Configures semantic-release behavior.

**Key sections:**

#### Branches
```json
"branches": ["main"]
```
Only releases from `main` branch. Feature branches are ignored.

#### Repository URL
```json
"repositoryUrl": "https://github.com/rafitajaen/kakeibo.git"
```
Where to create releases.

#### Plugins

1. **@semantic-release/commit-analyzer**
   - Analyzes commits to determine next version
   - Uses conventional commits preset
   - Release rules:
     - `feat` → minor (0.x.0)
     - `fix`, `perf`, `revert`, `refactor` → patch (0.0.x)
     - Breaking changes → major (x.0.0)
     - `docs`, `style`, `chore`, `test`, `build`, `ci` → no release

2. **@semantic-release/release-notes-generator**
   - Generates release notes from commits
   - Groups by type: Features, Bug Fixes, Performance, etc.
   - Hides: docs, style, chore, test, build, ci

3. **@semantic-release/changelog**
   - Updates `CHANGELOG.md` with release notes
   - Appends to existing file (preserves history)

4. **@semantic-release/exec**
   - Executes custom command before release
   - Saves version to `.version` file (optional, for CI use)

5. **@semantic-release/npm**
   - Updates `package.json` version field
   - Does NOT publish to npm registry (`npmPublish: false`)

6. **@semantic-release/git**
   - Commits changes to repository
   - Assets: `CHANGELOG.md`, `package.json`, `package-lock.json`
   - Message: `chore(release): ${nextRelease.version} [skip ci]`

7. **@semantic-release/github**
   - Creates GitHub Release
   - Uploads release assets (if any)
   - Posts comments on related issues/PRs

#### Complete Configuration

See `.releaserc.json` in repository root for full configuration.

### `package.json`

**Relevant fields:**

```json
{
  "name": "kakeibo",
  "version": "0.0.0",  // Updated automatically by semantic-release
  "devDependencies": {
    "semantic-release": "^24.2.1",
    "@semantic-release/changelog": "^6.0.3",
    "@semantic-release/commit-analyzer": "^13.0.0",
    "@semantic-release/exec": "^6.0.3",
    "@semantic-release/git": "^10.0.1",
    "@semantic-release/github": "^11.0.1",
    "@semantic-release/npm": "^12.0.1",
    "@semantic-release/release-notes-generator": "^14.0.1"
  }
}
```

**Never manually edit `version` field** — semantic-release owns it.

---

## Release Process

### Normal Flow (Feature Merge)

**Scenario:** Developer creates a feature, opens PR, gets approval, merges to `main`.

**Steps:**

1. **Feature branch:**
   ```bash
   git checkout -b feat/wallets-create-endpoint
   # ... implement feature ...
   git commit -m "feat(wallets): add wallet creation endpoint"
   git push origin feat/wallets-create-endpoint
   ```

2. **Pull Request:**
   - Open PR against `main`
   - Quality gates run (`quality.yml`)
   - Reviewer approves
   - PR merged (squash, merge, or rebase — all supported)

3. **Post-merge (automatic):**
   - Push to `main` triggers `release.yml`
   - `semantic-release` job runs:
     - Analyzes commit: `feat(wallets): ...`
     - Determines next version: `v0.1.0` → `v0.2.0` (minor bump)
     - Updates `package.json`: `"version": "0.2.0"`
     - Generates `CHANGELOG.md` entry
     - Creates commit: `chore(release): 0.2.0 [skip ci]`
     - Creates tag: `v0.2.0`
     - Pushes commit + tag
     - Creates GitHub Release

4. **Verification:**
   - Check: Repository → Releases → `v0.2.0` should exist
   - Check: `CHANGELOG.md` contains new entry
   - Check: `package.json` shows `"version": "0.2.0"`
   - Check: Git history shows release commit

### Version Bump Examples

| Commits Since Last Tag | Next Version | Reason |
|------------------------|--------------|--------|
| `feat(api): add endpoint` | v0.1.0 → v0.2.0 | Minor bump (feat) |
| `fix(api): handle null` | v0.2.0 → v0.2.1 | Patch bump (fix) |
| `feat(api)!: change schema` + `BREAKING CHANGE:` | v0.2.1 → v1.0.0 | Major bump (breaking) |
| `docs(readme): update` | No release | Docs don't trigger releases |
| `feat(api): add X`<br>`fix(api): fix Y` | v0.1.0 → v0.2.0 | Highest type wins (feat > fix) |

### Commit Message Format (Recap)

```
type(scope): description

[optional body]

[optional footer]
```

**Types that trigger releases:**
- `feat` → minor
- `fix`, `perf`, `revert`, `refactor` → patch
- Footer `BREAKING CHANGE:` → major

**Types that don't trigger releases:**
- `docs`, `style`, `chore`, `test`, `build`, `ci`

**Breaking changes:**
```
feat(api)!: change user schema

BREAKING CHANGE: User.Id type changed from Guid to Guid7.
All existing user IDs must be migrated.
```
The `!` after `type(scope)` is optional but recommended. The footer `BREAKING CHANGE:` is required.

---

## Troubleshooting

### Error: "The operation was canceled"

**Cause:** GitHub Actions timeout (default 6 hours).

**Solution:**
- Check workflow logs for actual error
- Usually not a timeout — look for earlier failure

### Error: "ENOENT: no such file or directory, open '.version'"

**Cause:** `@semantic-release/exec` plugin can't create `.version` file.

**Solution:**
- This is cosmetic — `.version` file is optional
- Safe to ignore (doesn't affect versioning)

### Error: "A tag already exists for version X.Y.Z"

**Cause:** Tag already exists (manual tag or previous run).

**Solution:**
- Delete the tag: `git tag -d vX.Y.Z && git push origin :refs/tags/vX.Y.Z`
- Or let semantic-release calculate next version (it will skip to v0.3.0 if v0.2.0 exists)

### Error: "No release type found for 8 commits"

**Cause:** All commits since last tag are types that don't trigger releases (docs, chore, etc.).

**Solution:**
- This is expected behavior
- No release will be created
- Next `feat` or `fix` commit will trigger a release

### Workflow doesn't run after merge

**Checks:**
1. ✅ Is `if: true` in `semantic-release` job?
2. ✅ Is `GH_TOKEN` configured in Secrets?
3. ✅ Does the PAT have `repo` + `workflow` scopes?
4. ✅ Did the commit have `[skip ci]` in message? (semantic-release commits have this intentionally)
5. ✅ Check Actions tab → Release workflow → View logs

### Release created but Docker images not pushed

**Expected behavior:** Docker jobs are **intentionally disabled** (`&& false`).

**To enable:**
Change condition in `.github/workflows/release.yml`:
```yaml
# From:
if: needs.semantic-release.outputs.new_release_published == 'true' && false

# To:
if: needs.semantic-release.outputs.new_release_published == 'true' && true
```

**When to enable:**
- `build-push-api`: When Identity module is complete and API is production-ready
- `build-push-app`: When `sites/Kakeibo.App` exists and frontend is production-ready
- `build-push-email`: When Email service is production-ready

### CHANGELOG.md has wrong content

**Cause:** Semantic-release generates notes from commit messages.

**Solution:**
- Improve commit messages going forward
- **Never manually edit CHANGELOG.md** — it will be overwritten
- If a release note is wrong, the commit message was wrong

---

## Maintenance

### Updating Semantic-Release

**When:** Major versions of semantic-release or plugins are released.

**How:**
```bash
# Check for updates
bun update semantic-release

# Or update specific plugin
bun update @semantic-release/changelog
```

**Test:**
- Dry-run in a branch (semantic-release has `--dry-run` flag)
- Verify release notes generation
- Merge and verify first real release

### Modifying Release Rules

**File:** `.releaserc.json`

**Example:** Make `refactor` trigger minor instead of patch:

```json
{
  "releaseRules": [
    { "type": "refactor", "release": "minor" }  // Changed from "patch"
  ]
}
```

**Test:** Create a test branch, make a `refactor` commit, observe version bump.

### Changing CHANGELOG Format

**File:** `.releaserc.json` → `@semantic-release/release-notes-generator` config

**Example:** Show `docs` commits in CHANGELOG:

```json
{
  "presetConfig": {
    "types": [
      { "type": "docs", "section": "Documentation", "hidden": false }
    ]
  }
}
```

**Result:** Docs commits now appear in release notes.

---

## Summary

**What semantic-release does:**
- ✅ Analyzes commits
- ✅ Calculates next version
- ✅ Updates `package.json`
- ✅ Generates `CHANGELOG.md`
- ✅ Creates Git tag
- ✅ Creates GitHub Release
- ✅ Pushes changes back to repository

**What you do:**
- ✅ Write conventional commits
- ✅ Merge PRs to `main`
- ✅ Verify releases in GitHub

**What you DON'T do:**
- ❌ Manually bump versions
- ❌ Manually edit CHANGELOG
- ❌ Manually create tags
- ❌ Manually create releases

**Key takeaway:** Follow conventional commits format → semantic-release handles the rest.
