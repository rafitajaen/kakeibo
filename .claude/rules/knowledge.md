# Knowledge Base

Lessons learned and gotchas discovered during development. Each entry has a unique ID (`KB-xxx`) with its own file in `.claude/references/knowledge/`.

## Entries

| ID | Title | Discovered | Affects |
|----|-------|-----------|---------|
| [KB-002](../references/knowledge/KB-002.md) | Prefer `[MemberNotNullWhen]` over null-forgiving operators | 2026-02-13 | All `*.cs` under `src/` |
| [KB-010](../references/knowledge/KB-010.md) | Migration from Modular Monolith to Simple Monolith | 2024 | `src/`, `Kakeibo.slnx` |
| [KB-012](../references/knowledge/KB-012.md) | EF Core global query filters must use mapped columns, not computed properties | 2026-02-27 | `*Configuration.cs` under `Persistence/Configurations/` |
| [KB-013](../references/knowledge/KB-013.md) | AsNoTracking must be explicit on all read-only EF Core queries | 2026-03-07 | `*Handler.cs` under `Features/` |

## Adding New Entries

1. Choose the next available `KB-xxx` ID (sequential, no gaps within a series).
2. Create `.claude/references/knowledge/KB-xxx.md` with: ID header, **Discovered** date, **Affects** scope, description, bad/good examples (table or code blocks), and a bolded **Rule** line.
3. Add a row to the table above linking to the new file.
