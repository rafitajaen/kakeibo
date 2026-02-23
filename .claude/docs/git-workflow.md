# Git Workflow

Exhaustive reference for all git operations, branching strategy, commit conventions, CI integration, and release processes for the Kakeibo platform.

---

## Table of Contents

1. [Branching Strategy](#1-branching-strategy)
2. [Commit Message Convention](#2-commit-message-convention)
3. [Valid Scopes](#3-valid-scopes)
4. [Pull Request Process](#4-pull-request-process)
5. [Code Review Guidelines](#5-code-review-guidelines)
6. [Release Process](#6-release-process)
7. [Hotfix Process](#7-hotfix-process)
8. [Revert Strategy](#8-revert-strategy)
9. [Git Configuration](#9-git-configuration)
10. [Commit Best Practices](#10-commit-best-practices)
11. [GitHub Actions Integration](#11-github-actions-integration)
12. [Pre-commit Hooks (lefthook)](#12-pre-commit-hooks-lefthook)
13. [Common Workflows](#13-common-workflows)
14. [Troubleshooting](#14-troubleshooting)

---

## 1. Branching Strategy

### Model: GitHub Flow (Trunk-Based)

Kakeibo uses **GitHub Flow** -- a trunk-based development model where `main` is always deployable. There is exactly one long-lived branch: `main`. All work happens on short-lived feature branches that merge back into `main` via pull request.

```
main ──●──●──●──●──●──●──●──●──●──
        \       /   \     /
         feat-1     fix-2
```

### Core Rules

- **`main` is always deployable.** Every commit on `main` has passed all quality gates.
- **No direct commits to `main`.** All changes go through a pull request. No exceptions -- not even "small fixes" or documentation updates.
- **No long-lived feature branches.** Branches should live hours to days, not weeks. If a feature takes longer than a few days, break it into smaller increments.
- **One logical change per branch.** A branch implements a single feature, fix, or improvement. If two changes are unrelated, they belong on separate branches.

### Branch Naming Convention

Branch names follow the pattern: `{type}/{short-description}`

| Prefix | When to use | Example |
|--------|-------------|---------|
| `feature/` | New functionality or capability | `feature/add-budget-alerts` |
| `fix/` | Bug fix (production or development) | `fix/wallet-balance-drift` |
| `refactor/` | Code restructuring without behavior change | `refactor/extract-debt-calculator` |
| `docs/` | Documentation-only changes | `docs/add-api-contracts` |
| `test/` | Adding or improving tests | `test/wallet-integration-tests` |
| `chore/` | Maintenance, dependency updates, CI config | `chore/update-dotnet-sdk` |

**Naming rules:**
- Use lowercase letters, numbers, and hyphens only
- Keep descriptions short (2-5 words)
- Use hyphens as word separators, never underscores or camelCase
- Do not include issue numbers in the branch name (reference them in the PR instead)

**Good branch names:**
```
feature/recurring-transaction-generation
fix/split-percentage-rounding
refactor/simplify-outbox-processor
docs/update-infrastructure-guide
test/budget-exceeded-event-handler
chore/bump-efcore-to-10
```

**Bad branch names:**
```
my-branch                    # No type prefix
feature/AddBudgetAlerts      # camelCase
fix/issue_42                 # Underscore + no description
feature/complete-overhaul-of-the-entire-transaction-module  # Too long
FEATURE/budget-alerts        # Uppercase prefix
```

### Protected Branch Rules for `main`

The `main` branch is protected with the following rules configured in GitHub repository settings:

| Rule | Setting |
|------|---------|
| Require pull request before merging | Enabled |
| Required approvals | 1 minimum |
| Dismiss stale PR reviews on new pushes | Enabled |
| Require status checks to pass before merging | Enabled |
| Required status checks | `quality-api`, `quality-app`, `quality-email`, `quality-docker` |
| Require branches to be up to date before merging | Enabled |
| Require linear history | Enabled (squash merge only) |
| Allow force pushes | Disabled |
| Allow deletions | Disabled |

---

## 2. Commit Message Convention

### Format: Conventional Commits

All commits must follow the [Conventional Commits](https://www.conventionalcommits.org/) specification. This is enforced by `commitlint` via a `commit-msg` lefthook hook -- commits that do not conform are rejected locally before they reach the remote.

### Structure

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

**Rules (enforced by `commitlint.config.ts`):**
- `type` is required (see types below)
- `scope` is optional but recommended (must be from the valid scope list)
- `description` is required, max 100 characters
- Description must not be empty
- First letter of description is lowercase
- No period at the end of the description

### Types

| Type | SemVer Effect | When to use |
|------|---------------|-------------|
| `feat` | Minor bump | A new feature or capability visible to users |
| `fix` | Patch bump | A bug fix |
| `refactor` | None | Code restructuring without changing external behavior |
| `docs` | None | Documentation only (markdown, comments, docstrings) |
| `style` | None | Formatting, whitespace, missing semicolons (no logic change) |
| `test` | None | Adding or correcting tests |
| `chore` | None | Maintenance tasks, dependency updates, CI/CD changes |
| `perf` | Patch bump | Performance improvement without changing functionality |
| `ci` | None | CI/CD configuration changes (GitHub Actions, lefthook) |

### Breaking Changes

Breaking changes are indicated in two ways (both should be used together):

1. **Exclamation mark** after the type/scope: `feat(api)!: redesign authentication flow`
2. **`BREAKING CHANGE:` footer** in the commit body explaining what breaks and how to migrate

Breaking changes trigger a **major** version bump regardless of the commit type.

```
feat(api)!: replace cookie-based auth with bearer token header

The API no longer sets HttpOnly cookies for authentication.
All clients must send the access token in the Authorization header.

BREAKING CHANGE: Authentication tokens are no longer stored in cookies.
Clients must include `Authorization: Bearer <token>` header on every
authenticated request. The `/api/auth/refresh` endpoint now returns
the new token in the response body instead of setting a cookie.

Closes #42
```

### Complete Examples

**Simple feature:**
```
feat(wallets): add wallet archiving endpoint
```

**Bug fix with scope:**
```
fix(transactions): correct rounding in percentage splits
```

**Refactoring:**
```
refactor(budgets): extract spending calculator to separate class
```

**Documentation:**
```
docs(api): add API contracts for wallet endpoints
```

**Style/formatting:**
```
style(app): apply oxfmt formatting to dashboard components
```

**Test:**
```
test(identity): add integration tests for password recovery flow
```

**Chore/maintenance:**
```
chore(deps): bump EntityFrameworkCore to 10.0.1
```

**Performance:**
```
perf(transactions): add composite index on wallet_id + date columns
```

**CI/CD:**
```
ci: add quality-docker job to validate Dockerfile builds
```

**No scope (cross-cutting change):**
```
chore: update .editorconfig with primary constructor enforcement
```

**Multi-paragraph commit with body and footer:**
```
feat(recurring): implement automatic transaction generation

Background job runs daily via Hangfire, scans active recurring patterns,
and generates transactions for patterns due on the current date. Each
generated transaction flows through the standard recording pipeline
(balance update, event publishing, audit logging).

The job is idempotent -- running it multiple times for the same day
produces no duplicate transactions. Pattern occurrences are tracked
with a composite key of (pattern_id, occurrence_date).

Closes #15
```

**Breaking change:**
```
feat(api)!: change wallet balance from float to decimal

All wallet balance fields in API responses are now returned as string-encoded
decimals to prevent floating-point precision loss in JavaScript clients.

BREAKING CHANGE: The `balance` field in wallet API responses changed from
`number` to `string`. Clients must parse the string to their preferred
numeric type. Example: `"1234.56"` instead of `1234.56`.
```

### Bad Commit Messages (and Why)

```
# Too vague -- what was updated?
fix: update stuff

# Missing type
wallet balance fix

# Description too long (over 100 chars)
feat(wallets): add the ability for users to create shared wallets and invite other users to collaborate on expenses together

# Capital letter in description
feat(api): Add new endpoint

# Period at end
fix(transactions): correct balance drift.

# Past tense (use imperative mood)
feat(goals): added milestone notifications

# Type not in allowed list
feature(wallets): add archiving
```

---

## 3. Valid Scopes

Scopes are defined in `commitlint.config.ts` and enforced by the `commit-msg` hook. Using a scope outside this list will cause the commit to be rejected.

### Complete Scope List

| Scope | When to use |
|-------|-------------|
| `app` | Changes to the Vue.js web app (`sites/Kakeibo.App/`) |
| `api` | Changes to the .NET API (`src/`) -- cross-module or API-level changes |
| `email` | Changes to the email renderer service (`services/Kakeibo.Email/`) |
| `docs` | Documentation files (`kakeibo/`, project docs) |
| `infra` | Infrastructure configuration (Docker, docker-compose, nginx, CI workflows) |
| `deps` | Dependency updates (NuGet packages, npm packages, Bun lockfile) |
| `release` | Release-related changes (CHANGELOG, version bumps) |
| `mobile` | Changes to the mobile app (future `sites/Kakeibo.Mobile/`) |
| `skills` | Changes to Claude skill files (`.claude/skills/`) |
| `roadmap` | Changes to roadmap/planning documents (`.claude/roadmap-*`) |

### Scope Selection Guidelines

**When the change touches a single area**, use the most specific scope:
```
feat(app): add budget progress bar component
fix(api): correct wallet balance recalculation on transaction delete
docs: update git workflow documentation
```

**When the change is cross-cutting across the API**, use `api`:
```
refactor(api): rename Options suffix on all configuration classes
```

**Multi-scope commits are not allowed.** If a change touches multiple areas, pick the most relevant scope -- the one that best describes the primary intent of the change:
```
# Good -- primary change is in the API, even though docs were updated too
feat(api): add budget exceeded event publishing

# Bad -- don't try to combine scopes
feat(api,app): add budget exceeded event and notification UI
```

If a change truly affects two unrelated areas equally, split it into two commits on the same branch.

**Scope is optional.** For truly cross-cutting changes that don't fit any scope, omit it:
```
chore: update .editorconfig with new analyzer rules
ci: add caching to GitHub Actions quality gates
```

### Module-Specific Scopes (Not in commitlint -- Use `api`)

The business modules (wallets, transactions, budgets, goals, recurring, identity, notifications, auditing) do not have dedicated scopes in `commitlint.config.ts`. When committing changes to a specific module, use the `api` scope and mention the module in the description:

```
feat(api): add wallet archiving endpoint to wallets module
fix(api): correct debt calculation in wallets module
test(api): add integration tests for budget handler
```

---

## 4. Pull Request Process

### When to Create a PR

**Always.** Every change to `main` goes through a pull request. There are no exceptions for:
- "Trivial" fixes (typos, whitespace)
- Documentation updates
- Configuration changes
- Dependency bumps
- CI/CD modifications

### PR Title Format

The PR title must follow the same Conventional Commits format as commit messages. Since Kakeibo uses **squash merge**, the PR title becomes the final commit message on `main`:

```
feat(wallets): add wallet archiving and unarchiving
fix(transactions): correct percentage split rounding to nearest cent
docs: add git workflow documentation
```

### PR Description Template

Every PR must include a description following this structure:

```markdown
## Summary

Brief description of what this PR does and why. 1-3 sentences maximum.

## Changes

- Bullet point list of specific changes made
- Each bullet should describe a discrete modification
- Include file paths for non-obvious changes

## Testing

- [ ] Unit tests added/updated
- [ ] Integration tests added/updated (if applicable)
- [ ] Manual testing performed (describe scenarios)
- [ ] All existing tests pass locally

## Screenshots

(If the change affects UI -- include before/after screenshots)

## Related Issues

Closes #123
Related to #456
```

**Example PR description:**

```markdown
## Summary

Adds wallet archiving functionality. Users can archive wallets they no longer
actively use, hiding them from the default view while preserving all data.
Archived wallets can be unarchived at any time.

## Changes

- Added `ArchiveWalletEndpoint` and `UnarchiveWalletEndpoint` in Wallets module
- Added `ArchivedAt` nullable timestamp to `Wallet` entity (replaces boolean)
- Added `WalletArchivedEvent` integration event to Contracts
- Updated `ListWalletsHandler` to exclude archived wallets by default
- Added `?includeArchived=true` query parameter to list endpoint
- Added FluentValidation for archive/unarchive requests

## Testing

- [x] Unit tests for `ArchiveWalletHandler` and `UnarchiveWalletHandler`
- [x] Integration test for archive → list → unarchive → list flow
- [x] Manual testing via Scalar API docs
- [x] All existing tests pass locally

## Related Issues

Closes #28
```

### Draft PRs

Use draft PRs for work in progress:
- Mark a PR as draft when you want early feedback but the work is not complete
- Draft PRs do not trigger review requests
- Draft PRs still run quality gate checks (useful for validating your approach)
- Convert to "Ready for review" when all changes are complete and tests pass

```bash
# Create a draft PR
gh pr create --title "feat(wallets): add archiving" --body "WIP" --draft

# Convert draft to ready
gh pr ready <pr-number>
```

### Required Checks

Before a PR can be merged, all required status checks must pass:

| Check | What it validates |
|-------|-------------------|
| `quality-api` | .NET restore, format check, build, unit tests, architecture tests |
| `quality-app` | Bun install, lint, unit tests, build |
| `quality-email` | Bun install, typecheck, lint, tests |
| `quality-docker` | All Dockerfiles build successfully |

If any check fails, the PR cannot be merged. Fix the issue, push new commits, and wait for checks to pass.

### Code Review Requirements

- **Minimum 1 approval** required before merging
- Stale reviews are dismissed when new commits are pushed (reviewer must re-approve)
- The PR author should not approve their own PR
- Reviews from any repository collaborator count

### Merge Strategy: Squash and Merge Only

Kakeibo uses **squash and merge** exclusively. This means:
- All commits on the feature branch are combined into a single commit on `main`
- The PR title becomes the commit message on `main`
- Individual branch commits do not appear in `main` history
- This produces a clean, linear history on `main`

**When merging via GitHub UI:**
1. Select "Squash and merge" (this should be the only option if repository settings are correct)
2. Verify the commit message matches Conventional Commits format
3. The commit message defaults to the PR title -- verify it is correct
4. Click "Confirm squash and merge"

**When merging via CLI:**
```bash
gh pr merge <pr-number> --squash --delete-branch
```

### Post-Merge Cleanup

After merging, delete the feature branch:
- GitHub can auto-delete branches after merge (enable in repository settings)
- Or delete manually: `git push origin --delete feature/my-branch`
- Clean up local tracking: `git fetch --prune`

---

## 5. Code Review Guidelines

### Self-Review Before Requesting Review

Before marking a PR as ready for review, the author must:

1. **Read the diff completely** -- review every file changed as if you were the reviewer
2. **Run quality checks locally** -- `bun run check:api` and `bun run check:app` (as applicable)
3. **Run tests locally** -- ensure all tests pass
4. **Remove debug artifacts** -- no `console.log`, no `TODO` comments without issue references, no commented-out code
5. **Verify the PR description** is complete and accurate
6. **Check for secrets** -- no `.env` values, API keys, or credentials in the diff

### Reviewer Checklist

When reviewing a PR, evaluate against these criteria:

**Correctness:**
- [ ] Does the code do what the PR description says?
- [ ] Are edge cases handled (null, empty, boundary values)?
- [ ] Are error paths handled gracefully (Result pattern, proper HTTP status codes)?
- [ ] Do domain invariants remain intact (balance accuracy, debt calculations)?

**Security:**
- [ ] No secrets, credentials, or API keys in the code
- [ ] Input validation present (FluentValidation for endpoints)
- [ ] Authorization checks in place (`.RequireAuthorization()`)
- [ ] No SQL injection vectors (parameterized queries via EF Core)
- [ ] No mass assignment vulnerabilities (explicit mapping, not `AutoMapper`)

**Architecture:**
- [ ] Follows vertical slice pattern (endpoint + handler + validator per feature)
- [ ] No cross-module references (modules communicate via Contracts only)
- [ ] Naming conventions followed (TD-009 through TD-013)
- [ ] Primary constructors used (mandatory.md Rule 8)
- [ ] NodaTime used instead of DateTime (TD-004)
- [ ] Guid7 used instead of Guid.CreateVersion7() (TD-005)

**Tests:**
- [ ] New functionality has corresponding tests
- [ ] Tests follow existing patterns (xUnit v3, Testcontainers for integration)
- [ ] Tests are independent (no shared mutable state between tests)
- [ ] Test names describe the scenario being tested
- [ ] Testcontainers tests include Docker skip guard (KB-008)

**Performance:**
- [ ] No N+1 query patterns (use `.Include()` or projection)
- [ ] Database queries filter at the database level, not in memory
- [ ] Pagination used for list endpoints
- [ ] No unnecessary allocations in hot paths

**Documentation:**
- [ ] Non-trivial methods have summary comments (TD-012)
- [ ] Public API changes documented
- [ ] Breaking changes clearly noted

### Review Turnaround Time SLA

| Priority | Target Response Time |
|----------|---------------------|
| Standard PR | Within 24 hours |
| Hotfix PR | Within 2 hours |
| Draft PR (feedback request) | Within 48 hours |

### Blocking vs Non-Blocking Comments

**Blocking comments** (Request Changes):
- Security vulnerabilities
- Incorrect business logic (wrong balance calculation, missing validation)
- Architecture violations (cross-module reference, missing Contracts type)
- Missing tests for new functionality
- Breaking changes without proper notation

**Non-blocking comments** (Approve with suggestions):
- Style preferences beyond what linters enforce
- Minor naming suggestions
- Alternative implementation approaches
- Documentation improvements
- Performance suggestions for non-critical paths

Prefix non-blocking comments with `nit:` or `suggestion:` to signal they do not block merge:
```
nit: Consider extracting this LINQ chain into a named method for readability.

suggestion: You could use a switch expression here instead of if/else.
```

### When to Approve vs Request Changes

**Approve** when:
- All blocking criteria are met
- You have only non-blocking suggestions
- The code is correct, secure, and well-tested

**Request Changes** when:
- Any blocking issue exists
- Tests are missing for new functionality
- A security concern is present
- Architecture rules are violated

**Comment** (without approving or requesting changes) when:
- You have questions but cannot evaluate correctness yet
- You need clarification before making a decision
- You have reviewed part of the PR and will return for the rest

---

## 6. Release Process

### Automatic Releases via semantic-release

Kakeibo uses **semantic-release** to automate the entire release process. When commits are pushed (merged) to `main`, the `release.yml` GitHub Actions workflow:

1. Analyzes all commits since the last release
2. Determines the version bump (patch, minor, major) based on commit types
3. Generates or updates `CHANGELOG.md`
4. Creates a GitHub Release with release notes
5. Builds and pushes Docker images to Docker Hub

### Version Bump Logic

| Commit Type(s) | Version Bump | Example |
|----------------|--------------|---------|
| `fix`, `perf` | **Patch** (0.0.x) | `1.2.3` → `1.2.4` |
| `feat` | **Minor** (0.x.0) | `1.2.3` → `1.3.0` |
| Any type with `BREAKING CHANGE` or `!` | **Major** (x.0.0) | `1.2.3` → `2.0.0` |
| `docs`, `style`, `refactor`, `test`, `chore`, `ci` | **No release** | No version bump, no release |

**Important:** Only `feat`, `fix`, and `perf` types (and breaking changes) trigger a new release. All other commit types produce no release even when merged to `main`.

### CHANGELOG Generation

`semantic-release` generates `CHANGELOG.md` automatically from commit messages. The changelog groups entries by type:

```markdown
## [1.3.0] - 2026-02-21

### Features
- **wallets:** add wallet archiving and unarchiving (#28)
- **recurring:** implement automatic transaction generation (#15)

### Bug Fixes
- **transactions:** correct percentage split rounding to nearest cent (#31)

### Performance Improvements
- **transactions:** add composite index on wallet_id + date columns (#35)
```

### Docker Image Tagging

When the `release.yml` workflow runs on `main`, it builds and pushes Docker images with two tags:

| Tag | Purpose | Example |
|-----|---------|---------|
| `latest` | Always points to the most recent build | `username/kakeibo-api:latest` |
| `sha-{git-sha}` | Immutable tag for specific commit | `username/kakeibo-api:sha-a1b2c3d` |

Images pushed to Docker Hub:
- `<username>/kakeibo-api`
- `<username>/kakeibo-app`
- `<username>/kakeibo-email`

### Release Workflow

```
Developer merges PR to main
    → GitHub Actions: release.yml triggers
    → semantic-release analyzes commits
    → If releasable commits found:
        → Bump version
        → Update CHANGELOG.md
        → Create GitHub Release with notes
        → Build Docker images
        → Push to Docker Hub (latest + sha-{sha})
    → If no releasable commits:
        → No release created
        → No images pushed
```

### Manual Releases

Manual releases are not part of the standard workflow. If a manual release is needed (emergency), follow the hotfix process (Section 7).

---

## 7. Hotfix Process

### When to Use a Hotfix

A hotfix is a fast-tracked change for a **critical production bug** that:
- Causes data corruption (incorrect balances, lost transactions)
- Breaks authentication (users cannot log in)
- Causes service downtime (API crashes, unhandled exceptions)
- Exposes a security vulnerability

Non-critical bugs follow the standard PR process.

### Hotfix Workflow

```
1. Create hotfix branch from main
2. Implement the fix (minimal change)
3. Add tests proving the fix
4. Create PR with [HOTFIX] prefix
5. Fast-track review (2-hour SLA)
6. Squash merge to main
7. semantic-release handles the rest
8. Verify fix in production
```

**Step-by-step commands:**

```bash
# 1. Ensure you have the latest main
git fetch origin
git checkout -b fix/critical-balance-drift origin/main

# 2. Implement the minimal fix
# ... make changes ...

# 3. Add tests
# ... add regression test ...

# 4. Commit and push
git add src/Kakeibo.Modules.Wallets/Features/RecalculateBalance/RecalculateBalanceHandler.cs
git add tests/Kakeibo.Modules.Wallets.Tests/Features/RecalculateBalance/RecalculateBalanceHandlerTests.cs
git commit -m "fix(api): correct balance drift caused by concurrent split updates"
git push -u origin fix/critical-balance-drift

# 5. Create PR with hotfix label
gh pr create \
  --title "fix(api): correct balance drift caused by concurrent split updates" \
  --body "## [HOTFIX] Critical balance drift

## Summary
Concurrent split updates on shared wallets could cause balance drift due to
missing optimistic concurrency check.

## Changes
- Added concurrency token to Wallet entity
- Added retry logic in RecalculateBalanceHandler

## Testing
- [x] Regression test for concurrent split scenario
- [x] All existing tests pass

## Impact
Users with shared wallets may have experienced small balance discrepancies.
A data reconciliation script may be needed." \
  --label "hotfix"

# 6. After fast-track review and merge, semantic-release creates a patch release
# 7. Verify the fix in production
```

### Post-Hotfix Verification

After a hotfix is deployed:
1. Monitor application logs for the specific error that triggered the hotfix
2. Verify the fix resolves the issue (manual testing in production)
3. Check that no new errors were introduced
4. If data corruption occurred, evaluate whether a data reconciliation script is needed
5. Create a follow-up issue for any root cause investigation needed

---

## 8. Revert Strategy

### When to Revert

Revert a merged PR when:
- The change introduces a regression that was not caught by tests
- The change causes production issues that cannot be quickly fixed forward
- The change was merged by mistake (wrong branch, incomplete work)
- A dependency update causes unexpected failures

### Decision Tree: Revert vs Fix Forward

```
Production issue detected after merge to main
    │
    ├─ Can you fix it in < 30 minutes?
    │   ├─ Yes → Fix forward (new PR with fix)
    │   └─ No → Revert
    │
    ├─ Is it causing data corruption?
    │   └─ Yes → Revert immediately, then investigate
    │
    ├─ Is it causing downtime?
    │   └─ Yes → Revert immediately, then investigate
    │
    └─ Is it a minor issue (UI glitch, non-critical path)?
        └─ Yes → Fix forward (standard PR process)
```

### Revert Commit Message Format

```
revert: <original commit message>

This reverts commit <sha>.

<Explanation of why the revert is needed>
```

**Example:**
```
revert: feat(wallets): add wallet archiving and unarchiving

This reverts commit a1b2c3d4e5f6.

The archiving feature causes a null reference exception when listing
wallets for users who have never created a wallet. Reverting until
the handler properly handles the empty wallet case.
```

### Revert Commands

**Revert via CLI:**
```bash
# Find the commit SHA to revert
git log --oneline -10

# Create a revert commit on a new branch
git checkout -b revert/wallet-archiving origin/main
git revert <commit-sha> --no-edit
git push -u origin revert/wallet-archiving

# Create PR for the revert
gh pr create \
  --title "revert: feat(wallets): add wallet archiving and unarchiving" \
  --body "Reverting due to null reference in ListWalletsHandler for new users."
```

**Revert via GitHub UI:**
1. Go to the merged PR
2. Click "Revert" button
3. This creates a new PR with the revert -- review and merge it

### After Reverting

1. Create an issue documenting what went wrong and why
2. Fix the original problem on a new branch
3. Include the missing test case that would have caught the bug
4. Submit a new PR with both the fix and the regression test

---

## 9. Git Configuration

### Required Setup

```bash
# Set your identity (required for commits)
git config --global user.name "Your Name"
git config --global user.email "your-email@example.com"
```

### GPG Signing (Recommended)

GPG signing is recommended but not required. Signed commits show a "Verified" badge on GitHub.

```bash
# Generate a GPG key (if you don't have one)
gpg --full-generate-key
# Choose: RSA and RSA, 4096 bits, key does not expire

# List your keys
gpg --list-secret-keys --keyid-format=long

# Configure git to use your key
git config --global user.signingkey <YOUR_KEY_ID>
git config --global commit.gpgsign true

# Export your public key and add it to GitHub
gpg --armor --export <YOUR_KEY_ID>
# Copy the output to: GitHub > Settings > SSH and GPG keys > New GPG key
```

### Recommended .gitconfig

```ini
[user]
    name = Your Name
    email = your-email@example.com

[core]
    editor = vim
    autocrlf = input
    whitespace = fix

[init]
    defaultBranch = main

[pull]
    rebase = true

[push]
    autoSetupRemote = true
    default = current

[fetch]
    prune = true

[merge]
    conflictstyle = zdiff3

[diff]
    algorithm = histogram
    colorMoved = default

[rebase]
    autoStash = true
    updateRefs = true

[rerere]
    enabled = true

[alias]
    # Status and log
    s = status --short --branch
    l = log --oneline -20
    lg = log --graph --oneline --decorate -20
    ll = log --graph --pretty=format:'%Cred%h%Creset -%C(yellow)%d%Creset %s %Cgreen(%cr) %C(bold blue)<%an>%Creset' --abbrev-commit -20

    # Branch operations
    co = checkout
    cb = checkout -b
    br = branch -vv
    bd = branch -d
    bD = branch -D

    # Commit operations
    cm = commit -m
    ca = commit --amend --no-edit
    cam = commit --amend -m

    # Diff
    d = diff
    ds = diff --staged
    dw = diff --word-diff

    # Stash
    sl = stash list
    sp = stash pop
    ss = stash push -m

    # Remote
    f = fetch --prune
    pl = pull --rebase
    ps = push

    # Cleanup
    cleanup = "!git branch --merged main | grep -v main | xargs -r git branch -d"
```

### Editor Configuration

```bash
# Use VS Code as git editor and diff tool
git config --global core.editor "code --wait"
git config --global diff.tool vscode
git config --global difftool.vscode.cmd 'code --wait --diff $LOCAL $REMOTE'
git config --global merge.tool vscode
git config --global mergetool.vscode.cmd 'code --wait $MERGED'
```

---

## 10. Commit Best Practices

### Atomic Commits

Each commit should represent **one logical change**. A logical change is the smallest unit of work that makes sense on its own:

**Good (atomic):**
```
# Commit 1: Add the entity
feat(api): add ArchivedAt property to Wallet entity

# Commit 2: Add the endpoint
feat(api): add archive wallet endpoint

# Commit 3: Add tests
test(api): add tests for wallet archiving
```

**Bad (too large):**
```
# One massive commit with entity + endpoint + tests + UI + docs
feat: implement wallet archiving feature
```

**Bad (too small):**
```
# These should be one commit
fix: add missing semicolon
fix: fix typo in previous commit
fix: actually fix the typo this time
```

### Commit Message Quality: Why, Not What

The diff shows **what** changed. The commit message should explain **why** it changed.

**Bad (restates the diff):**
```
fix(api): change MaxLength from 100 to 500
```

**Good (explains the reason):**
```
fix(api): increase description max length to match business constraint

The business constraints document specifies transaction descriptions
can be up to 500 characters, but the validator was set to 100.
```

### When to Amend vs New Commit

**Amend** when:
- You just committed and immediately noticed a typo or missing file
- The commit has NOT been pushed to the remote yet
- You want to update the commit message

```bash
# Add a forgotten file to the last commit
git add src/forgotten-file.cs
git commit --amend --no-edit

# Fix the commit message
git commit --amend -m "feat(api): correct commit message"
```

**New commit** when:
- The previous commit has already been pushed
- The change is logically separate from the previous commit
- You are fixing something found during code review (reviewers need to see the fix)

### Interactive Rebase for Cleanup (Before PR Review Only)

Before requesting review, you may clean up your branch history with interactive rebase. This is useful for squashing "fixup" commits or reordering commits for clarity.

```bash
# Rebase the last 5 commits interactively
git rebase -i HEAD~5

# Or rebase all commits since branching from main
git rebase -i main
```

**Rules:**
- Only rebase commits that have NOT been reviewed yet
- Never rebase after someone has started reviewing (it invalidates their review context)
- Since Kakeibo uses squash merge, branch commit history does not affect `main` -- rebase is purely for reviewer convenience

### Force Push Policy

| Target | Force push allowed? |
|--------|---------------------|
| `main` | **NEVER** -- force push is disabled by branch protection |
| Own feature branch (before review) | Yes -- `git push --force-with-lease` |
| Own feature branch (during review) | Avoid -- push new commits instead so reviewer can see incremental changes |
| Someone else's branch | **NEVER** -- unless explicitly asked by the branch owner |

**Always use `--force-with-lease`** instead of `--force`. It refuses to push if the remote has commits you have not fetched, preventing accidental overwrite of someone else's work:

```bash
# Safe force push (refuses if remote has new commits)
git push --force-with-lease

# Dangerous force push (NEVER use this)
# git push --force  # DO NOT USE
```

---

## 11. GitHub Actions Integration

### Workflow Files

| File | Trigger | Purpose |
|------|---------|---------|
| `.github/workflows/quality.yml` | `pull_request` to `main` | Run all quality gate checks |
| `.github/workflows/release.yml` | `push` to `main` | Build and push Docker images to Docker Hub |

### Quality Workflow (`quality.yml`)

Triggered on every pull request targeting `main`. All jobs must pass before the PR can be merged.

```yaml
name: Quality Gates
on:
  pull_request:
    branches: [main]

jobs:
  quality-api:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/Directory.Packages.props') }}
          restore-keys: ${{ runner.os }}-nuget-
      - run: dotnet restore Kakeibo.slnx
      - run: dotnet format Kakeibo.slnx --verify-no-changes --no-restore
      - run: dotnet build Kakeibo.slnx --no-restore --configuration Release
      - run: dotnet test Kakeibo.slnx --no-build --configuration Release

  quality-app:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: oven-sh/setup-bun@v2
      - run: cd sites/Kakeibo.App && bun install --frozen-lockfile
      - run: cd sites/Kakeibo.App && bun run lint:check
      - run: cd sites/Kakeibo.App && bun run test:unit -- --run
      - run: cd sites/Kakeibo.App && bun run build

  quality-email:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: oven-sh/setup-bun@v2
      - run: cd services/Kakeibo.Email && bun install --frozen-lockfile
      - run: cd services/Kakeibo.Email && bun run typecheck
      - run: cd services/Kakeibo.Email && bun run lint
      - run: cd services/Kakeibo.Email && bun run test

  quality-docker:
    runs-on: ubuntu-latest
    needs: [quality-api, quality-app, quality-email]
    steps:
      - uses: actions/checkout@v4
      - run: docker build -f src/Kakeibo.Api/Dockerfile -t kakeibo-api:test .
      - run: docker build -f sites/Kakeibo.App/Dockerfile -t kakeibo-app:test ./sites/Kakeibo.App
      - run: docker build -f services/Kakeibo.Email/Dockerfile -t kakeibo-email:test ./services/Kakeibo.Email
```

### Release Workflow (`release.yml`)

Triggered when commits are pushed to `main` (after PR merge). Builds and pushes Docker images.

```yaml
name: Release
on:
  push:
    branches: [main]

jobs:
  build-push:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKER_HUB_USERNAME }}
          password: ${{ secrets.DOCKER_HUB_TOKEN }}

      - uses: docker/setup-buildx-action@v3

      # Build and push API
      - uses: docker/build-push-action@v6
        with:
          context: .
          file: src/Kakeibo.Api/Dockerfile
          push: true
          tags: |
            ${{ secrets.DOCKER_HUB_USERNAME }}/kakeibo-api:latest
            ${{ secrets.DOCKER_HUB_USERNAME }}/kakeibo-api:sha-${{ github.sha }}
          cache-from: type=gha
          cache-to: type=gha,mode=max

      # Build and push App
      - uses: docker/build-push-action@v6
        with:
          context: ./sites/Kakeibo.App
          file: sites/Kakeibo.App/Dockerfile
          push: true
          tags: |
            ${{ secrets.DOCKER_HUB_USERNAME }}/kakeibo-app:latest
            ${{ secrets.DOCKER_HUB_USERNAME }}/kakeibo-app:sha-${{ github.sha }}
          cache-from: type=gha
          cache-to: type=gha,mode=max

      # Build and push Email
      - uses: docker/build-push-action@v6
        with:
          context: ./services/Kakeibo.Email
          file: services/Kakeibo.Email/Dockerfile
          push: true
          tags: |
            ${{ secrets.DOCKER_HUB_USERNAME }}/kakeibo-email:latest
            ${{ secrets.DOCKER_HUB_USERNAME }}/kakeibo-email:sha-${{ github.sha }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

### Required Status Checks

Configure in GitHub repository settings (Settings > Branches > Branch protection rules > `main`):

| Check Name | Required |
|------------|----------|
| `quality-api` | Yes |
| `quality-app` | Yes |
| `quality-email` | Yes |
| `quality-docker` | Yes |

### Secrets Management

Required repository secrets (Settings > Secrets and variables > Actions):

| Secret | Description | How to obtain |
|--------|-------------|---------------|
| `DOCKER_HUB_USERNAME` | Docker Hub account username | Docker Hub account settings |
| `DOCKER_HUB_TOKEN` | Docker Hub access token | Docker Hub > Account Settings > Security > Access Tokens |

**Security rules:**
- Never hardcode secrets in workflow files
- Never echo secrets in CI logs (GitHub automatically masks known secrets)
- Use the minimum required permissions for tokens (read/write for Docker Hub push)
- Rotate tokens periodically (at least annually)

### Workflow Permissions

Default workflow permissions should be set to **read-only** in repository settings (Settings > Actions > General > Workflow permissions). Specific permissions are granted per workflow:

```yaml
permissions:
  contents: read      # Read repository code
  packages: write     # Push Docker images (if using GitHub Container Registry)
```

---

## 12. Pre-commit Hooks (lefthook)

### Overview

Kakeibo uses [lefthook](https://github.com/evilmartians/lefthook) to run pre-commit hooks locally. Hooks are defined in `lefthook.yml` at the repository root.

### Installation

```bash
# Install lefthook (if not already installed via bun run setup)
bun install

# Install git hooks
bunx lefthook install
```

### Hook: `commit-msg` (Commitlint Enforcement)

Runs after you write your commit message. Validates the message against Conventional Commits format using the rules in `commitlint.config.ts`.

```yaml
# lefthook.yml
commit-msg:
  commands:
    commitlint:
      run: bunx commitlint --edit {1}
```

**What it checks:**
- Type is present and valid (`feat`, `fix`, `refactor`, `docs`, `style`, `test`, `chore`, `perf`, `ci`)
- Scope (if provided) is in the allowed list (`app`, `api`, `email`, `docs`, `infra`, `deps`, `release`, `mobile`, `skills`, `roadmap`)
- Description is present and not empty
- Description is 100 characters or fewer

**When it rejects:**
```
$ git commit -m "updated wallet handler"
⧗   input: updated wallet handler
✖   subject may not be empty [subject-empty]
✖   type may not be empty [type-empty]

✖   found 2 problems, 0 warnings

husky - commit-msg script failed (code 1)
```

### Hook: `pre-commit` (Lint and Format Check)

Runs before the commit is created. Checks staged files for lint errors and formatting issues. Runs four checks in parallel across the web app and mobile app:

```yaml
# lefthook.yml
pre-commit:
  parallel: true
  commands:
    oxlint:
      root: "sites/Kakeibo.App/"
      glob: "*.{ts,tsx,vue,js,jsx}"
      run: bunx oxlint --deny-warnings {staged_files}

    oxfmt-check:
      root: "sites/Kakeibo.App/"
      glob: "*.{ts,tsx,vue,js,jsx,css,json}"
      run: bunx oxfmt --check {staged_files}

    oxlint-mobile:
      root: "sites/Kakeibo.Mobile/"
      glob: "*.{ts,tsx,vue,js,jsx}"
      run: bunx oxlint --deny-warnings {staged_files}

    oxfmt-check-mobile:
      root: "sites/Kakeibo.Mobile/"
      glob: "*.{ts,tsx,vue,js,jsx,css,json}"
      run: bunx oxfmt --check {staged_files}
```

**Important:** The hook runs in **check mode only** -- it never auto-fixes files. You must run the auto-fix commands yourself before committing.

### Pre-Commit Workflow

1. Make your changes
2. Run auto-fix commands for the projects you touched:
   ```bash
   # Frontend (Kakeibo.App)
   bun run app:format && bun run app:lint

   # Email service
   bun run email:format
   ```
3. Re-stage any files modified by the formatters:
   ```bash
   git add <modified-files>
   ```
4. Commit -- the pre-commit hook will now pass

**Why re-stage?** The formatters modify files on disk but git only includes the version that was staged. If you skip re-staging, the hook still sees the pre-fix staged version and rejects the commit.

### Bypassing Hooks (Emergency Only)

In rare emergencies, you can bypass hooks with `--no-verify`:

```bash
git commit --no-verify -m "fix(api): emergency production hotfix

Bypassing pre-commit hooks due to production emergency.
Hook bypass reason: CI environment mismatch causing false positive in oxlint.
Follow-up: #123 to investigate and resolve the oxlint false positive."
```

**Rules for bypassing:**
- Only use for genuine emergencies (production is down, critical security fix)
- Always document the bypass reason in the commit message body
- Create a follow-up issue to address whatever caused the hook failure
- Never bypass `commitlint` -- if you cannot write a valid commit message, something is wrong
- The CI quality gates will still run on the PR -- hooks are a local convenience, not the final gate

### Troubleshooting Hook Failures

**"commitlint not found":**
```bash
# Reinstall dependencies
bun install

# Verify commitlint is available
bunx commitlint --version
```

**"oxlint command failed":**
```bash
# Run the auto-fix first
cd sites/Kakeibo.App && bunx oxlint --fix .
# Re-stage fixed files
git add <fixed-files>
```

**"oxfmt --check failed":**
```bash
# Run the formatter
cd sites/Kakeibo.App && bunx oxfmt .
# Re-stage formatted files
git add <formatted-files>
```

**Hooks not running at all:**
```bash
# Reinstall lefthook hooks
bunx lefthook install

# Verify hooks are installed
ls -la .git/hooks/
# Should see commit-msg and pre-commit symlinks
```

**Hook runs but on wrong files:**
```bash
# Verify staged files
git diff --staged --name-only

# If files are not in sites/Kakeibo.App/ or sites/Kakeibo.Mobile/,
# the pre-commit hooks will not run (they only target those directories)
```

---

## 13. Common Workflows

### Workflow 1: Feature Development

The most common workflow for adding new functionality.

```bash
# 1. Start from latest main
git fetch origin
git checkout -b feature/add-budget-alerts origin/main

# 2. Develop incrementally with atomic commits
# ... implement endpoint ...
git add src/Kakeibo.Modules.Budgets/Features/CheckBudgetAlert/
git commit -m "feat(api): add budget alert checking endpoint"

# ... implement handler ...
git add src/Kakeibo.Modules.Budgets/Features/CheckBudgetAlert/CheckBudgetAlertHandler.cs
git commit -m "feat(api): implement budget alert threshold logic"

# ... add tests ...
git add tests/Kakeibo.Modules.Budgets.Tests/
git commit -m "test(api): add tests for budget alert threshold calculation"

# 3. Push to remote
git push -u origin feature/add-budget-alerts

# 4. Create PR
gh pr create \
  --title "feat(api): add budget alert threshold checking" \
  --body "## Summary
Adds endpoint and handler for checking budget alert thresholds.
Publishes BudgetWarningEvent when spending exceeds 80% of budget limit.

## Changes
- Added CheckBudgetAlertEndpoint with GET /api/budgets/{id}/alerts
- Added CheckBudgetAlertHandler with 80% threshold logic
- Added BudgetWarningEvent to Contracts

## Testing
- [x] Unit tests for threshold calculation
- [x] Integration test for event publishing
- [x] All existing tests pass"

# 5. Wait for quality gates + code review
# 6. Address review feedback with new commits
git add <changed-files>
git commit -m "fix(api): address review feedback on budget alert handler"
git push

# 7. After approval, squash merge via GitHub UI or CLI
gh pr merge --squash --delete-branch

# 8. Clean up local branch
git checkout main
git pull
git branch -d feature/add-budget-alerts
```

### Workflow 2: Bug Fix

```bash
# 1. Create fix branch
git fetch origin
git checkout -b fix/wallet-balance-drift origin/main

# 2. Write a failing test first (TDD)
git add tests/Kakeibo.Modules.Wallets.Tests/Features/RecalculateBalance/
git commit -m "test(api): add regression test for concurrent balance update"

# 3. Implement the fix
git add src/Kakeibo.Modules.Wallets/
git commit -m "fix(api): prevent balance drift from concurrent split updates"

# 4. Push and create PR
git push -u origin fix/wallet-balance-drift
gh pr create \
  --title "fix(api): prevent balance drift from concurrent split updates" \
  --body "## Summary
Fixes balance drift when two users update splits simultaneously on a shared wallet.

## Root Cause
Missing optimistic concurrency check on Wallet entity allowed two concurrent
SaveChangesAsync calls to both succeed with stale balance values.

## Changes
- Added ConcurrencyToken to Wallet entity
- Added retry logic in balance recalculation handler
- Added regression test for concurrent update scenario

## Testing
- [x] Regression test (fails before fix, passes after)
- [x] All existing tests pass"

# 5. Standard review and merge process
```

### Workflow 3: Documentation Update

```bash
# 1. Create docs branch
git fetch origin
git checkout -b docs/add-api-contracts origin/main

# 2. Write documentation
git add kakeibo/api-contracts.md
git commit -m "docs: add API contracts for wallet and transaction endpoints"

# 3. Push and create PR
git push -u origin docs/add-api-contracts
gh pr create \
  --title "docs: add API contracts for wallet and transaction endpoints" \
  --body "## Summary
Adds complete API contract documentation for wallet and transaction modules.

## Changes
- Added kakeibo/api-contracts.md with endpoint specifications
- Covers all CRUD operations for wallets and transactions
- Includes request/response schemas and error codes"

# 4. Review and merge (quality gates still run but docs-only changes pass trivially)
```

### Workflow 4: Dependency Update

```bash
# 1. Create dependency update branch
git fetch origin
git checkout -b chore/update-efcore-10.0.1 origin/main

# 2. Update the dependency
# For .NET: edit Directory.Packages.props
# For npm/bun: bun update <package>

# 3. Run tests to verify nothing breaks
dotnet test Kakeibo.slnx

# 4. Commit
git add Directory.Packages.props
git commit -m "chore(deps): bump EntityFrameworkCore to 10.0.1"

# 5. Push and create PR
git push -u origin chore/update-efcore-10.0.1
gh pr create \
  --title "chore(deps): bump EntityFrameworkCore to 10.0.1" \
  --body "## Summary
Updates Entity Framework Core from 10.0.0 to 10.0.1 (patch release).

## Changes
- Updated PackageVersion in Directory.Packages.props
- No code changes required

## Testing
- [x] All existing tests pass
- [x] API builds and starts successfully

## Release Notes
https://github.com/dotnet/efcore/releases/tag/v10.0.1"
```

### Workflow 5: Updating a Branch with Latest Main

When your branch falls behind `main` and needs to be updated:

```bash
# Option A: Rebase (preferred -- keeps linear history)
git fetch origin
git rebase origin/main

# If conflicts occur:
# 1. Resolve conflicts in your editor
# 2. Stage resolved files: git add <file>
# 3. Continue rebase: git rebase --continue
# 4. Force push (safe because it's your branch): git push --force-with-lease

# Option B: Merge (when rebase would be too complex)
git fetch origin
git merge origin/main

# Resolve any conflicts, then push normally
git push
```

**Prefer rebase** for branches with a small number of commits. **Use merge** when the branch has many commits or has been shared with other developers.

---

## 14. Troubleshooting

### Merge Conflicts

**Prevention:**
- Keep branches short-lived (merge within 1-3 days)
- Rebase on `main` frequently during development
- Communicate with teammates about overlapping work areas

**Resolution:**
```bash
# 1. Update your branch with latest main
git fetch origin
git rebase origin/main

# 2. Git will stop at each conflict. For each conflicted file:
#    - Open the file in your editor
#    - Look for conflict markers: <<<<<<<, =======, >>>>>>>
#    - Resolve by keeping the correct code
#    - Remove all conflict markers

# 3. Stage the resolved file
git add <resolved-file>

# 4. Continue the rebase
git rebase --continue

# 5. If the conflict is too complex and you want to start over:
git rebase --abort

# 6. After resolving all conflicts, force push your branch
git push --force-with-lease
```

**Common conflict scenarios and resolutions:**

| Scenario | Resolution |
|----------|------------|
| Both branches modified the same handler | Keep your changes, integrate the other branch's intent |
| Both branches added new features to the same module registration | Include both registrations |
| Migration conflicts (two new migrations) | Delete your migration, re-run `dotnet ef migrations add` after rebase |
| `Directory.Packages.props` conflicts | Accept both version bumps, verify compatibility |

### Failed CI Checks

**`quality-api` fails:**
```bash
# Check which step failed in the GitHub Actions log

# Format check failed:
dotnet format Kakeibo.slnx --verify-no-changes
# If it fails, run the formatter and push:
dotnet format Kakeibo.slnx
git add -A && git commit -m "style(api): apply dotnet format" && git push

# Build failed:
dotnet build Kakeibo.slnx --configuration Release
# Fix compilation errors, commit, and push

# Tests failed:
dotnet test Kakeibo.slnx --configuration Release
# Run the failing test locally, fix, commit, and push
```

**`quality-app` fails:**
```bash
# Lint failed:
cd sites/Kakeibo.App && bun run lint:check
# Fix lint errors:
cd sites/Kakeibo.App && bun run lint
git add <fixed-files> && git commit -m "style(app): fix lint errors" && git push

# Tests failed:
cd sites/Kakeibo.App && bun run test:unit -- --run
# Fix failing tests, commit, and push

# Build failed:
cd sites/Kakeibo.App && bun run build
# Fix TypeScript errors, commit, and push
```

**`quality-docker` fails:**
```bash
# Build the Dockerfile locally to reproduce the error
docker build -f src/Kakeibo.Api/Dockerfile -t kakeibo-api:test .
# Common causes:
# - Missing COPY for a new .csproj file (new module added)
# - Missing COPY for new source directory
# - Package restore failure (check Directory.Packages.props)
```

### Accidental Commit to Main

If you accidentally committed directly to `main` (branch protection should prevent this, but in case of misconfiguration):

```bash
# Option 1: If you haven't pushed yet
# Move the commit to a new branch
git branch fix/accidental-commit
git reset --hard HEAD~1
git checkout fix/accidental-commit
git push -u origin fix/accidental-commit
# Create a PR from the new branch

# Option 2: If you already pushed (and branch protection was bypassed)
# Create a revert commit
git checkout main
git revert HEAD
git push origin main
# Then create a proper branch and PR with the original changes
```

### Lost Commits (Git Reflog Recovery)

If you accidentally lost commits (bad rebase, wrong reset, deleted branch):

```bash
# View the reflog -- git records every HEAD movement
git reflog

# Output looks like:
# a1b2c3d HEAD@{0}: rebase: abort
# e4f5g6h HEAD@{1}: rebase: checkout origin/main
# i7j8k9l HEAD@{2}: commit: feat(api): add wallet archiving endpoint  <-- your lost commit

# Recover the lost commit
git checkout -b recovery/wallet-archiving i7j8k9l

# Or cherry-pick it onto your current branch
git cherry-pick i7j8k9l
```

**Reflog retention:** Git keeps reflog entries for 90 days by default. After that, unreferenced commits may be garbage collected.

### Detached HEAD State

```bash
# You're in detached HEAD state (not on any branch)
git status
# HEAD detached at a1b2c3d

# If you made commits in detached HEAD:
git checkout -b recovery/my-work

# If you just want to get back to your branch:
git checkout main
# or
git checkout feature/my-branch
```

### Stale Local Branches

```bash
# List all local branches and their tracking status
git branch -vv

# Delete branches whose remote tracking branch is gone
git fetch --prune
git branch -vv | grep ': gone]' | awk '{print $1}' | xargs git branch -d

# Or use the alias from the recommended .gitconfig
git cleanup
```

### Large Files Accidentally Committed

```bash
# If you haven't pushed yet, amend the commit
git rm --cached <large-file>
echo "<large-file>" >> .gitignore
git add .gitignore
git commit --amend --no-edit

# If you already pushed, you need to rewrite history
# WARNING: This rewrites history -- coordinate with your team
git filter-branch --force --index-filter \
  'git rm --cached --ignore-unmatch <large-file>' \
  --prune-empty --tag-name-filter cat -- --all

# Then force push (only to your feature branch, NEVER to main)
git push --force-with-lease
```

### Git LFS Issues

Kakeibo does not use Git LFS. If you encounter LFS-related errors, it likely means a dependency or tool installed LFS hooks. Remove them:

```bash
git lfs uninstall
```

---

*This document is the canonical reference for all git operations in the Kakeibo project. When in doubt about a git workflow, consult this document first. If a scenario is not covered, discuss with the team and update this document with the resolution.*
