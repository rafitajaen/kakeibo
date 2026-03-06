# money.backup — SQLite Database Analysis

Analysis of a `money.backup` SQLite3 file exported from an Android personal finance app.
The goal of this study is to fully understand the schema, data model, and contents to
inform a future data import into Kakeibo.

---

## 1. File Identity

| Property | Value |
|----------|-------|
| Format | SQLite 3.28.0 |
| File size | ~3.5 MB |
| Pages | 886 |
| Locale | Spanish (`es_ES` in `android_metadata`) |
| App type | Android personal finance tracker (Money Manager style) |
| Evidence | Spanish category/book names: "Copia de seguridad diaria", "Salario", "Comestibles", etc. |

---

## 2. Table Inventory

All table names are two-letter abbreviations — a common pattern in Android SQLite apps to
minimize storage on constrained devices.

| Table | Purpose | Row Count |
|-------|---------|-----------|
| `android_metadata` | Android system locale declaration | 1 |
| `ba` | Books (ledger instances / backup snapshots) | 24 |
| `cu` | Currencies | 0 (empty) |
| `ta` | Tags | 0 (empty) |
| `de` | Accounts AND categories combined (dual-purpose) | 750 |
| `bu` | Budget category list (mirrors `de`) | 750 |
| `bs` | Budget amounts | 0 (empty) |
| `tr` | Transactions | 45,139 |
| `pa` | Per-book settings / metadata | 24 |

**Key observations:**
- `cu`, `ta`, `bs` are structurally defined but never populated. Currency data is implicit (EUR).
- `de` serves double duty as both the accounts/wallets table and the categories table — distinguished by the `_ty` column.
- `bu` is a budget-related mirror of `de` with identical row count (750). Budgets were never configured (`bs` is empty).
- The 45,139 transaction rows include significant duplication across backup books (see Section 3).

---

## 3. Book Structure (`ba`)

The app supports multiple "books" (ledger instances). In this backup, 24 books exist but only
one contains live data — the rest are automated daily backup snapshots.

### Book Types

| `_ty` value | Meaning | Count |
|-------------|---------|-------|
| `1` | Primary active book | 1 |
| `2` | Export book | 1 |
| `4` | Daily backup copies ("Copia de seguridad diaria") | 22 |

### Key Books

| `_id` | Name | Type | Notes |
|-------|------|------|-------|
| 858 | (primary book) | 1 | **The only book to import.** Contains 1,946 unique transactions. |
| 868–888 | "Copia de seguridad diaria" | 4 | Daily automated backups. Data duplicated from book 858. |
| 889 | "Export" | 2 | Export artifact. |

### Column Reference (`ba`)

| Column | Type | Meaning |
|--------|------|---------|
| `_id` | INTEGER PK | Book ID |
| `_na` | TEXT | Book name |
| `_ty` | INTEGER | Book type (1=primary, 2=export, 4=daily backup) |
| `_da` | INTEGER | Creation timestamp (Unix milliseconds) |
| `_fi` | TEXT | Associated file path (nullable) |

### Deduplication Rule

All 45,139 transactions span 24 books. Filtering by `_b_i = 858` yields the 1,946 unique
transactions that represent real user data. All other books must be excluded from any import.

---

## 4. Accounts & Categories (`de`)

The `de` table is the most structurally unusual aspect of this schema. It combines what would
normally be two separate tables — accounts/wallets and expense categories — into a single table
differentiated by the `_ty` column.

### Column Reference (`de`)

| Column | Type | Meaning |
|--------|------|---------|
| `_id` | INTEGER PK | Entity ID (referenced by `tr._a_i` and `tr._d_i`) |
| `_b_i` | INTEGER | Book ID (foreign key → `ba._id`) |
| `_ty` | INTEGER | Entity type (see below) |
| `_na` | TEXT | Display name |
| `_co` | INTEGER | Android ARGB color (negative integer, 32-bit signed) |
| `_ic` | INTEGER | Icon index |
| `_de` | TEXT | Description / notes (often empty) |

### Entity Types (`_ty`)

| `_ty` | Meaning | Examples |
|-------|---------|---------|
| `0` | Account (wallet) or income source type | Rafa, Cristina, Común, Salario, Otro |
| `1` | Expense category | Hogar, Comestibles, Coche, Tabaco, … |
| `4` | Virtual aggregate "Todas las cuentas" | All-accounts roll-up (not a real account) |

**Important:** Accounts and income "types" (Salario, Otro) share `_ty=0`. They are distinguished
by their role in transactions — income types appear as `_d_i` in income transactions, while
wallets appear as `_a_i` (source). In practice, "Salario" and "Otro" are never the source of a
payment, only the destination/label for income entries.

### Accounts (Wallets) to Import

These are the `_ty=0` entries that function as real wallets:

| Name | Description |
|------|-------------|
| **Rafa** | Personal wallet for Rafa |
| **Cristina** | Personal wallet for Cristina |
| **Común** | Shared wallet (household expenses) |

The `_ty=4` "Todas las cuentas" entry must be excluded — it is a UI aggregate, not a real account.

### Expense Categories (25 custom)

All `_ty=1` entries in book 858. These map directly to Kakeibo custom categories.

---

## 5. Transaction Model (`tr`)

### Column Reference (`tr`)

| Column | Type | Meaning |
|--------|------|---------|
| `_id` | INTEGER PK | Transaction ID |
| `_b_i` | INTEGER | Book ID (foreign key → `ba._id`) |
| `_ty` | INTEGER | Transaction type: `0`=expense, `1`=income |
| `_a_i` | INTEGER | Source account (foreign key → `de._id`) |
| `_d_i` | INTEGER | Destination (foreign key → `de._id`) — category for expenses, income type for income |
| `_a_m` | TEXT | Source amount (string, decimal notation) |
| `_d_m` | TEXT | Destination amount (string, same as `_a_m` in single-currency setup) |
| `_da` | INTEGER | Transaction date (Unix milliseconds — divide by 1000 for Unix seconds) |
| `_co` | TEXT | Free-text description / notes |
| `_c_f` | INTEGER | Currency flag (always 0 — single currency) |
| `_c_id` | INTEGER | Category ID — **always empty/null; unused** |

### Transaction Types

| `_ty` | Flow | `_a_i` (source) | `_d_i` (destination) |
|-------|------|-----------------|----------------------|
| `0` | Expense | Rafa / Cristina / Común | Expense category (e.g., Hogar, Coche) |
| `1` | Income | Cristina / Rafa | Income type (Salario, Otro) |

**Category encoding:** The `_c_id` column exists in the schema but is never populated. Categories
are encoded entirely via the `_d_i` foreign key pointing to a `de._ty=1` entry. Any import logic
must use `_d_i` — never `_c_id` — to resolve the category.

**No transfers recorded:** There are no transfer-type transactions (which would typically require
a third `_ty` value or a pair of linked records). All transactions are either expenses or income.

### Date Conversion

```
Unix timestamp (ms) → divide by 1000 → Unix timestamp (seconds) → UTC datetime
```

Example: `1641081600000 / 1000 = 1641081600` → `2022-01-02 00:00:00 UTC`

Dates should be stored as NodaTime `Instant` in Kakeibo.

### Amount Format

Amounts are stored as TEXT (string) in decimal notation (e.g., `"45.50"`). During import,
parse as `decimal` before writing to Kakeibo. The currency is implicitly EUR throughout —
the `cu` table is empty and `_c_f` is always `0`.

---

## 6. Data Statistics (Book 858 Only)

All statistics below are scoped exclusively to `_b_i = 858`.

### Overview

| Metric | Value |
|--------|-------|
| Total transactions | 1,946 |
| Expense transactions | 1,899 |
| Income transactions | 47 |
| Date range | January 2022 — October 2025 |
| Future-dated entries | 2 (dated 2026 — likely data entry errors) |
| Total expenses | ~€71,455 |
| Total income | ~€59,184 |

### Transactions by Source Account

| Account | Transactions | Total Amount |
|---------|-------------|--------------|
| Común (shared) | 799 | ~€41,461 |
| Cristina | 782 | ~€23,986 |
| Rafa | 318 | ~€6,009 |

### Top Expense Categories

| Rank | Category | Transactions | Total Amount |
|------|----------|-------------|--------------|
| 1 | Hogar (household) | 159 | ~€15,442 |
| 2 | Comestibles (groceries) | 238 | ~€10,987 |
| 3 | Coche (car) | 133 | ~€10,008 |
| 4 | Londres (London trips) | 29 | ~€4,240 |
| 5 | Tabaco (tobacco) | 110 | ~€4,128 |
| 6 | Familia (family) | 142 | ~€3,704 |
| 7 | Teléfono (phone) | 139 | ~€3,079 |

### Income by Account

| Account | Income Received |
|---------|----------------|
| Cristina | ~€48,209 |
| Rafa | ~€10,975 |

Income transactions are sparse (47 total) — the app was used primarily as an expense tracker
with only major income entries (salary) recorded periodically.

---

## 7. Kakeibo Import Mapping

### Filter

```sql
WHERE _b_i = 858
```

Apply this filter to both `de` and `tr` tables to extract only primary book data.

### Wallets to Create

| Source (`de._ty=0`) | Kakeibo Wallet | Type |
|---------------------|----------------|------|
| Rafa | Rafa | Personal |
| Cristina | Cristina | Personal |
| Común | Común | Shared (Rafa + Cristina as members) |

Exclude: `_ty=4` "Todas las cuentas" — this is a virtual aggregate with no transactions.

### Categories to Create

- Import all 25 `de._ty=1` entries from book 858 as custom Kakeibo categories.
- Income "types" (`Salario`, `Otro`) from `de._ty=0` should map to Kakeibo's built-in
  income categories or be created as custom categories.

### Transaction Mapping

| Source column | Kakeibo field | Notes |
|---------------|---------------|-------|
| `tr._a_m` | `amount` | Parse TEXT → `decimal`. Negate for expenses. |
| `tr._da / 1000` | `date` | Unix seconds → `Instant` (UTC) |
| `tr._co` | `description` | Free-text notes; may be empty |
| `tr._ty` | `type` | `0` → Expense, `1` → Income |
| `tr._a_i` → `de._na` | `walletId` | Resolve source account name → wallet |
| `tr._d_i` → `de._na` | `categoryId` | Resolve destination → category |

### Currency

All amounts are EUR. Set currency at wallet creation — no per-transaction currency conversion needed.

### Data Quality Notes

- **2 future-dated transactions** (2026): Review manually before importing. Likely entry errors.
- **Description field (`_co`)** is inconsistently populated — many transactions have no description.
- **Rafa's wallet** has significantly fewer transactions (318 vs 782/799) — Rafa may have tracked
  expenses less consistently, or used a separate app.
- **Income is underrepresented** (47 transactions over 3.75 years) — the backup was used primarily
  for expense tracking. Net cash flow will appear negative in Kakeibo.
- **No transfers recorded** — the three wallets operated independently with no inter-wallet transfers
  in the source data.

---

## 8. SQLite Query Reference

Useful queries for validation during import development:

```sql
-- List all books
SELECT _id, _na, _ty, datetime(_da/1000, 'unixepoch') AS created FROM ba;

-- Count transactions per book
SELECT _b_i, COUNT(*) FROM tr GROUP BY _b_i ORDER BY COUNT(*) DESC;

-- List accounts and categories for book 858
SELECT _id, _ty, _na FROM de WHERE _b_i = 858 ORDER BY _ty, _na;

-- Expense summary by category (book 858)
SELECT d._na AS category, COUNT(*) AS tx_count, SUM(CAST(t._a_m AS REAL)) AS total
FROM tr t
JOIN de d ON t._d_i = d._id
WHERE t._b_i = 858 AND t._ty = 0
GROUP BY d._na
ORDER BY total DESC;

-- Income summary by account (book 858)
SELECT d._na AS account, COUNT(*) AS tx_count, SUM(CAST(t._a_m AS REAL)) AS total
FROM tr t
JOIN de d ON t._a_i = d._id
WHERE t._b_i = 858 AND t._ty = 1
GROUP BY d._na
ORDER BY total DESC;

-- Date range of data
SELECT
  datetime(MIN(_da)/1000, 'unixepoch') AS earliest,
  datetime(MAX(_da)/1000, 'unixepoch') AS latest
FROM tr WHERE _b_i = 858;

-- Transactions with no description
SELECT COUNT(*) FROM tr WHERE _b_i = 858 AND (_co IS NULL OR _co = '');
```
