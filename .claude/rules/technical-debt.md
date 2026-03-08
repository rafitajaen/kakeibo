# Technical Debt Rules

Knowledge base of technical debt patterns to detect and fix across the codebase. Each rule has a unique ID (`TD-xxx`) with its own file in `.claude/references/technical-debt/`. Used by the `/audit-tech-debt` command to generate automated audit reports.

**Severities:**
- **CRITICAL** — Violates a prohibited technology rule from `CLAUDE.md`. Must be fixed immediately.
- **WARNING** — Code smell that reduces maintainability, readability, or type safety. Should be fixed in current phase.
- **INFO** — Minor improvement opportunity. Fix when touching the file.

## Rules

| ID | Title | Severity | Category |
|----|-------|----------|----------|
| [TD-001](../references/technical-debt/TD-001.md) | Hardcoded Enumerator Strings | WARNING | Magic Strings & Constants |
| [TD-002](../references/technical-debt/TD-002.md) | Configuration Keys as Magic Strings | WARNING | Magic Strings & Constants |
| [TD-003](../references/technical-debt/TD-003.md) | Hardcoded Content Types | INFO | Magic Strings & Constants |
| [TD-004](../references/technical-debt/TD-004.md) | DateTime/DateTimeOffset Usage | CRITICAL | Prohibited API Usage |
| [TD-005](../references/technical-debt/TD-005.md) | Direct Guid.CreateVersion7 Usage | CRITICAL | Prohibited API Usage |
| [TD-006](../references/technical-debt/TD-006.md) | Duplicated Resource Names | WARNING | Resource & Configuration Duplication |
| [TD-007](../references/technical-debt/TD-007.md) | Inline Timeout and Duration Values | INFO | Resource & Configuration Duplication |
| [TD-008](../references/technical-debt/TD-008.md) | Magic Numbers | WARNING | Magic Strings & Constants |
| [TD-009](../references/technical-debt/TD-009.md) | Configuration Models Must Use Options Suffix | WARNING | Naming Conventions |
| [TD-010](../references/technical-debt/TD-010.md) | EF Core Entity Configurations Must Use Configuration Suffix | WARNING | Naming Conventions |
| [TD-011](../references/technical-debt/TD-011.md) | Endpoint Classes Must Use Endpoint Suffix | WARNING | Naming Conventions |
| [TD-012](../references/technical-debt/TD-012.md) | Non-Trivial Methods Must Be Commented | WARNING | Code Documentation |
| [TD-013](../references/technical-debt/TD-013.md) | Endpoint Input/Output Must Use {Operation}Request/{Operation}Response | WARNING | Naming Conventions |
| [TD-014](../references/technical-debt/TD-014.md) | Missing InternalsVisibleTo for Test Projects | WARNING | Project Configuration |
| [TD-015](../references/technical-debt/TD-015.md) | Monorepo Scripts Must Target Kakeibo.slnx | WARNING | Monorepo Script Alignment |
| [TD-016](../references/technical-debt/TD-016.md) | Redundant Boolean for Nullable Timestamp | WARNING | Redundant State |
| [TD-017](../references/technical-debt/TD-017.md) | Relative Path Imports Beyond Same Directory | WARNING | Import Conventions |
| [TD-018](../references/technical-debt/TD-018.md) | Non-Canonical Icon Library Usage | INFO | Icon Library Consistency |
| [TD-019](../references/technical-debt/TD-019.md) | Testcontainers Tests Must Skip When Docker Is Unavailable | WARNING | Test Infrastructure |
| [TD-020](../references/technical-debt/TD-020.md) | `.WithReuse(true)` Prohibited in Testcontainers | CRITICAL | Test Infrastructure |
| [TD-021](../references/technical-debt/TD-021.md) | .NET Code Must Be Checked Against the dotnet-modernize Skill | WARNING | .NET Modernization |

## Adding New Rules

1. Choose the next available `TD-xxx` ID within the appropriate category.
2. Create `.claude/references/technical-debt/TD-xxx.md` following the template: ID header, **Severity**, **Category**, description, bad/good examples (code blocks), **Applies to**, and **Detection patterns**.
3. Add a row to the table above linking to the new file.
