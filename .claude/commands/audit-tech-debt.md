---
description: "Audit C# code for technical debt: magic strings, magic numbers, prohibited patterns"
model: sonnet
allowed-tools: Read, Glob, Grep, Bash, Write
arguments:
  - name: scope
    description: "Directory scope to audit (default: 'src/')"
    required: false
---

Read-only auditor. Analyze C# files against rules in `roadmap/technical-debt.md` and write a report. NEVER modify source files.

**Language**: Communicate in Spanish. Report in English.

## Step 1: Discover

1. Read `roadmap/technical-debt.md` — parse all rules (ID, severity, detection patterns).
2. Read `CLAUDE.md` for project conventions.
3. Scope: `$ARGUMENTS.scope` if provided, otherwise `src/`.
4. Glob `{scope}**/*.cs`, exclude `obj/`, `bin/`, `Migrations/`.
5. Read each file.

## Step 2: Analyze

Evaluate every file against every rule from `roadmap/technical-debt.md`. For each violation record: rule ID, file, line(s), severity, code quote, fix description.

Use Grep to detect patterns efficiently — each rule in the document lists its detection patterns.

## Step 3: Generate Report

1. `mkdir -p reports/` and `date +%Y-%m-%d` via Bash.
2. Write to `reports/{DATE}-tech-debt-audit.md`:

```
# Technical Debt Audit
**Date**: {DATE} | **Scope**: {scope} | **Files**: {n} | **Rules**: `roadmap/technical-debt.md`

## Executive Summary
{2-3 sentences. Severity counts table.}

## Findings by Rule
| Rule | Severity | Occurrences |
...

## Per-file Findings
### `{file}`
| # | Rule | Severity | Line | Code | Fix |
...

## Refactoring Plan
Priority 1 (CRITICAL) → Priority 2 (WARNING) → Priority 3 (INFO)

## Checklist
- [ ] **[TD-xxx]** `{file}` (L{n}): {fix}
```

## Step 4: Notify User

Spanish summary: report path, severity counts, top 3 findings, checklist reminder.

## Rules

1. **Read-only.** Only write to `reports/`.
2. **All rules evaluated.** Read `roadmap/technical-debt.md` fresh every run — it is the single source of truth.
3. **Every finding in checklist.** Quote actual code with line numbers.
4. **Report is self-contained.** No need to look at source code to understand findings.
