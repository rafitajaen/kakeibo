# Knowledge Base

Lessons learned and gotchas discovered during development.

---

## KB-002: Prefer `[MemberNotNullWhen]` over null-forgiving operators

**Discovered:** 2026-02-13
**Affects:** All `*.cs` files under `src/`

When a type has a boolean property that implies the nullability of another member (e.g., `IsSuccess` implies `Error` is non-null when `false`), annotate the boolean with `[MemberNotNullWhen]` from `System.Diagnostics.CodeAnalysis`. This lets the compiler infer nullability through flow analysis and eliminates the need for the `!` operator.

| Bad | Good |
|-----|------|
| `if (result.IsFailure) return result.Error!;` | `if (result.IsFailure) return result.Error;` |
| `result.IsSuccess ? result.Value!.Id : ...` | `result.IsSuccess ? result.Value.Id : ...` |

**Rule:** Never suppress nullable warnings with `!` when the invariant can be expressed via `[MemberNotNullWhen]`. The `!` operator hides real bugs and defeats the purpose of nullable reference types.

---

## KB-012: EF Core global query filters must use mapped columns, not computed properties

**Discovered:** 2026-02-27
**Affects:** All `*Configuration.cs` files under `src/Kakeibo.Api/Persistence/Configurations/`

EF Core translates `HasQueryFilter` expressions to SQL at query time. Computed C# properties (expression-bodied members) are not mapped to any column, so EF Core cannot translate them and throws `InvalidOperationException` at runtime.

`Entity.IsDeleted` is defined as `=> DeletedAt is not null` — a pure C# expression with no column behind it. Using it in a query filter compiles fine but crashes on the first DB query.

| Bad | Good |
|-----|------|
| `builder.HasQueryFilter(u => !u.IsDeleted);` | `builder.HasQueryFilter(u => u.DeletedAt == null);` |

**Rule:** Always use the underlying mapped column (`DeletedAt == null`) in `HasQueryFilter`, never the computed boolean property (`IsDeleted`). The same applies to any other computed property derived from a column (e.g., `IsVerified => VerifiedAt is not null`).
