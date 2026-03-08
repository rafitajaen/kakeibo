# OneMoney — UI/UX Visual Study

> Reference study based on 23 screenshots captured on 2026-03-08. The goal is to document
> OneMoney's visual design, screen structure, and UX patterns as inspiration for the Kakeibo
> frontend. All observations are made from the screenshots only.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Global Layout & Navigation](#2-global-layout--navigation)
3. [Screen-by-Screen Analysis](#3-screen-by-screen-analysis)
   - 3.1 [Drawer Menu](#31-drawer-menu)
   - 3.2 [Profile](#32-profile)
   - 3.3 [Settings](#33-settings)
   - 3.4 [Data Management](#34-data-management)
   - 3.5 [Accounts (Cuentas)](#35-accounts-cuentas)
   - 3.6 [Categories (Categorías)](#36-categories-categorías)
   - 3.7 [Transactions (Transacciones)](#37-transactions-transacciones)
   - 3.8 [Summary (Resumen)](#38-summary-resumen)
4. [UI Patterns & Components](#4-ui-patterns--components)
5. [Color & Icon System](#5-color--icon-system)
6. [Key UX Decisions Worth Adopting in Kakeibo](#6-key-ux-decisions-worth-adopting-in-kakeibo)

---

## 1. Overview

**OneMoney** is a personal finance tracking app with a clean, mobile-first design. It follows
Material Design conventions loosely, preferring a lighter, more colourful aesthetic. Its most
distinctive feature is the heavy use of **colour-coded category icons** rendered as filled circles
with white glyphs — a visual system that recurs across every screen.

### Visual Language

| Property | Description |
|----------|-------------|
| Background | Pure white (`#FFFFFF`) for content areas; very light grey (`~#F5F5F5`) for page backgrounds |
| Primary accent | Dark indigo / blue used for active tab underlines, section titles, and interactive text |
| Expense colour | Pink/magenta (`~#E91E8C` or similar hot pink) |
| Income colour | Green/teal (`~#00BFA5`) |
| Destructive actions | Red (`~#F44336`) for delete/logout buttons |
| Typography | Sans-serif, medium weight for labels; large bold numbers for amounts |
| Amounts — negative/expense | Displayed in pink/red |
| Amounts — positive/income | Displayed in green/teal |

### Core Screens (Bottom Tab Bar)
1. **Accounts** (Cuentas)
2. **Categories** (Categorías)
3. **Transactions** (Transacciones)
4. **Summary** (Resumen)

---

## 2. Global Layout & Navigation

### Persistent App Header

Every main screen shares a three-zone top bar:

```
[ Avatar/Profile icon ]   [ Account count · Net balance ]   [ Action icon ]
```

- **Left**: Circular avatar/profile icon (outline style). Tapping opens the Drawer Menu.
- **Center**: Two lines:
  - Line 1: `"Cuentas · N"` — account count label, in accent blue
  - Line 2: Net total balance — in pink/red if negative, green if positive, large bold font
- **Right**: Context-sensitive action icon (search 🔍 on Transactions; `+` on Accounts; house icon on Categories/Summary)

### Period Selector Row (visible on Categories, Transactions, Summary)

A horizontal row directly below the header with three zones:

```
[ «« ]   [ ∞ SIEMPRE  ▾ ]   [ »» ]
```

- **Left `««`**: Navigate to previous period
- **Center**: Current period pill (pill-shaped rounded button, light grey fill). Tapping opens the Period Modal.
- **Right `»»`**: Navigate to next period
- Period options: Always (∞), Today, Week, Month, Year, Select day, Select range

### Bottom Tab Bar

Fixed at the bottom. Four tabs with icon + label. Active tab: icon turns filled/bold + background highlight pill appears.

| Tab | Icon style | Label |
|-----|-----------|-------|
| Accounts | Wallet (filled) | Cuentas |
| Categories | Pie chart | Categorías |
| Transactions | Receipt/list | Transacciones |
| Summary | Bar chart | Resumen |

### Floating Action Button (FAB)

- Visible on the **Transactions** screen.
- Large circular button, blue/indigo, positioned bottom-right above the tab bar.
- Icon: `+` (add new transaction).

### Drawer Menu

Slides in from the left over the current screen (the right portion of the screen remains visible, dimmed). Opened via the avatar icon.

---

## 3. Screen-by-Screen Analysis

---

### 3.1 Drawer Menu

**Trigger**: Tap the avatar/profile icon (top-left) on any main screen.

**Layout**: Left-side overlay drawer covering ~85% of screen width.

**Header section** (inside drawer):
- App logo: Circular gradient icon (blue→pink) with `1` numeral
- App name: `1Money` in bold
- Subtitle: Sync timestamp — `"Hoy, HH:MM"` with a cloud icon

**Menu items** (icon + label, full-width rows with generous padding):

| Icon | Label |
|------|-------|
| Crown | Perfil (Profile) |
| Gear | Configuración (Settings) |
| Stack of disks | Datos (Data) |
| Star | Califícanos (Rate us) |
| Headset | Soporte (Support) |
| Info circle | Acerca de (About) |

**Bottom-left**: A teal circular FAB with a heart-in-hands icon (charity/premium branding).

**Visual style**: White background. Menu items use a medium-dark text. No active state highlighting. Rows separated by implicit spacing (no dividers between items).

---

### 3.2 Profile Screen

**Navigation**: Drawer → Perfil

**Layout**: Standard settings list page with a back arrow (`←`) in the header.

**Top section — Sync status**:
- Cloud icon + `"Sincronización"` label
- Subtitle: last sync timestamp in accent blue (`"Hoy, HH:MM"`)
- Separated from the rest by a divider

**Profile section** (blue section heading `"Perfil"`):

| Icon | Row | Value |
|------|-----|-------|
| Crown (greyed) | Versión premium | `Licencia vitalicia` (greyed, already owned) |
| `@` | E-Mail | `user@gmail.com` (blue, tappable) |
| Exit arrow | Cerrar sesión | — |

**Danger zone** (below a divider):
- Trash icon + `"Borrar"` label in red — deletes account

---

### 3.3 Settings Screen

**Navigation**: Drawer → Configuración

**Layout**: Settings list with icon + label + current value (in blue below label).

**Section 1 — Appearance**:

| Icon | Setting | Current value |
|------|---------|---------------|
| Globe | Idioma (Language) | Predeterminado |
| Palette | Tema (Theme) | Claro (Light) |

**Section 2 — Behaviour** (separated by divider):

| Icon | Setting | Control |
|------|---------|---------|
| Arrow-in-circle | Pantalla de inicio (Start screen) | Tappable (current: Transacciones) |
| Gauge/pie | Presupuesto (Budget) | Toggle switch (ON) |
| Fingerprint | Código de acceso (Passcode) | Toggle switch (ON), subtitle: Inmediatamente |
| Bell | Notificaciones | Toggle switch (ON), subtitle: 20:00 |

**Section 3 — Finance** (separated by divider):

| Icon | Setting | Current value |
|------|---------|---------------|
| Dollar circle | Moneda predeterminada | Euro – € |
| `123` | Formato de moneda | −1.234.567,90 € |
| `7` in box | Primer día de la semana | Lunes (Monday) |
| `31` in box | El primer día del mes | 1 |

---

#### 3.3.1 Currency Picker (sub-screen)

**Navigation**: Settings → Moneda predeterminada

**Header**: `X` (close) + `"Moneda predeterminada"` title

**Tab bar** (3 tabs with icons):
- `$` — **Moneda principal** (Main currency) — active/underlined
- `₹` — **Otras monedas** (Other currencies)
- `Ƀ` — **Criptomonedas** (Crypto)

**Content**: Radio list of currencies. Each row: radio button + currency name (left) + symbol (right).
- Currently selected row: text turns blue, radio filled blue.
- Example currencies shown: Canadian Dollar, US Dollar, Australian Dollar, **Euro** ✓, Swiss Franc, British Pound, Russian Ruble, Japanese Yen, Chinese Yuan.

---

#### 3.3.2 Currency Format Modal

**Type**: Bottom sheet / modal dialog (white card with rounded top corners, appears over dimmed background).

**Header**: `123` icon + `"Formato de moneda"` title

**Content**: Scrollable radio list showing all format variants.
- Selected option highlighted with filled radio: `−1.234.567,90 €`
- Shows ~15 format variations combining: thousands separator (`.` or `,` or space), decimal separator (`.` or `,`), symbol position (suffix or prefix), negative indicator (prefix `−` or prefix `−€` or prefix `€ −`)

**Footer**: `OK` button (right-aligned, rounded pill, light lavender fill)

---

#### 3.3.3 Theme Screen

**Navigation**: Settings → Tema

**Layout**: Settings sub-page.

**Section 1**:

| Icon | Setting | Value |
|------|---------|-------|
| Gear-sun | Tema | Claro (Light) — tappable |
| Moon+ | Black theme | Toggle switch (OFF) |

**Section 2** — `Color` (blue section heading):
- 4×4 grid (+ 3 in last row = 15 total) of large filled circles, each representing a theme accent colour.
- Currently selected circle has a white border/ring around it.
- Available colours: purple, indigo, **dark blue** ✓, blue, sky blue, cyan, teal, green, lime green, yellow-green, yellow, orange, red, hot pink, brown.

---

#### 3.3.4 Start Screen Modal

**Type**: Bottom sheet / modal dialog.

**Header**: Arrow-in-circle icon + `"Pantalla de inicio"` title

**Content**: Radio list of available start screens:
- Cuentas
- Categorías
- **Transacciones** ✓ (selected)
- Presupuesto
- Resumen

**Footer**: `OK` button (right-aligned, light lavender pill)

---

### 3.4 Data Management Screen

**Navigation**: Drawer → Datos

**Layout**: Settings list page. Back arrow header.

**Section 1 — Reset**:
- Circular arrows icon + `"Reset data"` — resets all data

**Section 2 — Export** (blue heading `"Exportar"`):

| Icon | Action |
|------|--------|
| Download arrow | Import data |
| Upload arrow | Export data |
| CSV document | Exportar datos a CSV |

**Section 3 — Backup** (blue heading `"Copia de seguridad"`):
- ⚠️ Warning banner (red tint background, red `!` icon):
  > "Backups are stored locally on your device. When you delete or reinstall the app, the backups are deleted."
- `+ Crear copia de respaldo` (blue link-style)
- **Backup list** (each row shows):
  - Icon: clock/history or pin
  - Title: `"Copia de seguridad diaria"` or `"Export"`
  - Date + time
  - Stats: `N transacciones · N cuentas · N categorías`

**Backup types in list**:
- Automatic daily backups (clock icon)
- Manual exports (pin icon)

---

### 3.5 Accounts (Cuentas)

**Navigation**: Bottom tab 1

This screen has **two tabs** inside the content area:

#### 3.5.1 Accounts Tab

**Header**: Shows total count + net balance (negative = red)

**Tab bar** (inside content, below the global period row):
- `Cuentas` (active) | `Mis finanzas`

**Content area — "Cuentas" section heading** + total at right (red/pink):

Each account is a **list row**:
```
[ Coloured square icon ]  Account name
                          Balance (green = positive, red = negative)
```

- Icon: large square with rounded corners, solid fill colour, white emoji/glyph inside
- Example accounts:
  - `Cristina` — blue square, smiley face emoji, `24.033,67 €` (green)
  - `Rafa` — teal/green square, wallet icon, `4.918,43 €` (green)
  - `Común` — pink square, credit card icon, `−41.512,74 €` (red)

**No explicit account type labels** — types are distinguished only by icon glyph.

#### 3.5.2 My Finances Tab (Mis finanzas)

**Content**: A summary table with two columns:

```
|   | ACTIVOS      | DEUDAS        |
|---| 28.952,10 €  | 41.512,74 €   |
Net: 12.560,64 €
```

- Assets column: green amount
- Debts column: red/pink amount
- Net row below: pink (negative result)

#### 3.5.3 Account Action Bottom Sheet

**Trigger**: Long-press or tap on an account row.

**Layout**: Bottom sheet with the account header at top (full-width coloured card):
- Account icon + name + star icon (favourite toggle) on the right
- `"Saldo de cuenta"` label + balance amount below

**Action buttons** arranged in 2×3 grid (circular icon + label below):

| Icon | Label | Icon colour |
|------|-------|-------------|
| Pencil | Editar | Yellow |
| Circular arrows | Balance | Grey |
| Receipt | Transacciones | Teal |
| ↑ Arrow | Recarga (Add money) | Teal |
| ↓ Arrow | Retiro (Withdraw) | Pink/red |
| → Arrow | Transferencia | Grey |

#### 3.5.4 New Account Type Picker

**Trigger**: Tap `+` button in top-right of Accounts screen.

**Layout**: Bottom sheet titled `"Nueva cuenta"`.

Three account type options (each a full-width rounded card with illustration + title + subtitle):

| Illustration | Title | Subtitle |
|-------------|-------|---------|
| Wallet + coins | **Regular** | Efectivo, tarjeta, … |
| Hands holding cash | **Deuda** | Crédito, hipoteca, … |
| Globe/savings | **Ahorros** | Ahorro, meta, … |

#### 3.5.5 Account Filter Bottom Sheet

**Trigger**: Tapping a filter/funnel icon on the Mis finanzas tab.

**Layout**: Bottom sheet titled `"Filtro de cuentas"`.

**Content**:
- Blue section heading: `"Cuentas"`
- Grid of account cards (2 columns), each showing: icon + name + balance. All accounts selected = filled/coloured; deselected = greyed out.

**Footer buttons**:
- `Restablecer` (Reset) — left, greyed pill
- `Hecho` (Done) — right, solid blue pill with lines icon

---

### 3.6 Categories (Categorías)

**Navigation**: Bottom tab 2

#### 3.6.1 Category Grid View (main view)

**Header**: Same global header. Right icon: house shape (redirects somewhere).

**Period row**: `«« | ∞ SIEMPRE ▾ | »»`

**Content**: The defining visual element — a **radial donut chart** in the centre surrounded by **category icon circles** arranged around it.

**Donut chart**:
- Large ring chart, colour-coded segments matching each category's assigned colour
- Centre text: `"Gastos"` (or "Ingresos") label + total amount in pink + income total in green below
- Each segment's colour matches the surrounding icon circles

**Category circles** (arranged peripherally around the donut):
- Circle: large solid-fill coloured circle (matching donut segment colour)
- White icon glyph inside the circle
- Name label above
- Amount label below in matching colour

Observed categories with their colours:
| Category | Circle colour | Icon |
|----------|--------------|------|
| Comestibles (Groceries) | Blue | Basket/cart |
| Restaurantes | Dark navy | Fork + knife |
| Tiempo libre | Hot pink | Ticket/entertainment |
| Transporte | Orange/amber | Bus |
| Salud | Green | Heart with hands |
| Regalos | Red | Gift box |
| Familia | Purple | Smiley face |
| Compras | Orange | Shopping bag |
| Trabajo | Navy blue | Buildings |
| Hogar | Yellow/gold | House |
| Tabaco | Dark brown | Cigarette |
| Coche | Grey | Car |
| Teléfono | Teal | Phone |
| Viajes | Lime green | Ship/boat |
| Gobierno | Near-black | Building/government |

**Top-right corner**: small house/home icon (navigates to home or budget screen).

#### 3.6.2 Period Filter Modal

**Trigger**: Tap the period pill in the period row.

**Layout**: Bottom sheet titled `"Período"`.

**Options** in a 2×3 grid of cards:

| Card | Icon | Label | Subtitle |
|------|------|-------|---------|
| 1 | `···` | Seleccionar rango | date range shown |
| 2 (selected) | `∞` | **Siempre** | — |
| 3 | Calendar | Seleccione el día | — |
| 4 | `7` | Semana | date range |
| 5 | `1` | Hoy | today's date |
| 6 | `365` | Año | year label |
| 7 | `31` | Mes | month label |

- Selected option has a lavender/light purple fill background.
- Cards are rounded squares with the icon above and label below.

#### 3.6.3 Edit Categories Mode

**Navigation**: Top-right icon (house) → Edit categories, or a dedicated edit flow.

**Screen title**: `"Editar categorías"`

**Sub-tabs**:
- `↓ Gastos` (Expenses) — active, underlined in blue
- `↑ Ingresos` (Income)

**Content**: Same donut chart + category circle grid layout as the main view, but:
- A `+` placeholder circle (dashed border, grey) appears in the last position of the grid — tap to add a new category.
- Each existing category circle is tappable to edit it.

#### 3.6.4 Category Detail / Edit Screen

**Navigation**: Tap a category circle from Edit Categories mode.

**Screen title**: `"Categoría"` with `←` back + `⋮` overflow menu (top-right)

**Layout**:
- **Name field**: `"Nombre"` label + large editable text (`"Comestibles"`)
- **Icon preview**: Floating large rounded square (blue fill, white basket icon) — tappable to change icon
- **Section: Configuración** (blue heading):
  - Dollar circle icon + `"Categoría de Moneda"` + current currency (`Euro – €`)
- **Section: Subcategorias** (blue heading):
  - List of subcategories, each with: custom icon + name + `⋮` overflow menu
  - Examples: `Pescadería` (water drop icon), `Frutería` (clipboard icon)
  - `+ Añadir subcategoría` link at the bottom
- **Toggle**: `Archivar categoría` with a toggle switch (OFF by default)
- **Danger row**: `Borrar categoría` in red with trash icon

#### 3.6.5 Subcategory Context Menu

**Trigger**: Tap `⋮` on a subcategory row.

**Layout**: Small dropdown/popup anchored to the `⋮` button, rounded card:

Options:
1. Convertir a categoría (Convert to category)
2. Fusionar con subcategoría (Merge with subcategory)
3. Archivar (Archive)
4. Borrar (Delete)

#### 3.6.6 New Category Form

**Navigation**: Tap `+` circle in Edit Categories mode.

**Screen title**: `"Nueva categoría"` with `X` (close, left) + `"Hecho"` (Done, blue pill, right)

**Layout**:
- **Name field**: `"Nombre"` label + empty text input (cursor active, keyboard open)
- **Icon preview**: Floating rounded square (pink fill, shopping cart icon) — randomised default, tappable to change
- **Section: Configuración**:
  - `Categoría de Moneda` → `Euro – €`
- **Section: Subcategorias**:
  - `+ Añadir subcategoría` (no subcategories yet)

---

### 3.7 Transactions (Transacciones)

**Navigation**: Bottom tab 3

#### 3.7.1 Transaction List View

**Header**: Same global header. Right icon: 🔍 (search).

**Period row**: `«« | ∞ SIEMPRE ▾ | »»`

**Scheduled transactions banner** (collapsible, appears below period row):
- Collapsed state: `↑ | N transacciones agendadas | ↑` — a clickable row to expand/collapse
- Contains future/scheduled transactions (upcoming recurring payments)

**Transaction list** — grouped by date:

**Date group header**:
```
DD    DAY-OF-WEEK          Total for day (in pink)
      MONTH YEAR
```
- Day number is very large and bold, left-aligned
- Day of week + "MONTH YEAR" is smaller, right of the day number

**Transaction row**:
```
[ Category icon circle ]   Category name        Amount (pink)
                           [ Account icon ] Account name
                           Note/description (italic)
```

- Category icon: same coloured circle as Categories screen
- Account indicator: small account icon (square) + account name in grey — shows which account the transaction belongs to
- Note: shown in italic grey below account name
- Amount: right-aligned, pink/red for expenses

**Example transactions**:
- `Coche` category (grey car icon) · `Cristina` account · Note: `"Seguro Coche"` · `41,25 €`
- `Hogar` category (yellow house) · `Común` account · Note: `"Netflix"` · `13 €`
- `Teléfono` category (teal) · `Cristina` account · Note: `"Lowi"` · `16 €`
- `Teléfono` category (teal) · `Rafa` account · Note: `"Lowi"` · `12 €`

**FAB**: Large `+` button, blue/indigo, bottom-right, above the tab bar.

---

### 3.8 Summary (Resumen)

**Navigation**: Bottom tab 4

This is the richest data screen.

#### 3.8.1 Balance Header + Gastos/Ingresos Cards

Below the global header and period row:

```
Balance
−12.560,64 €         ← large, red/pink
```

Two side-by-side cards (full width together):
- **Left card** (pink fill): `"Gastos"` label + expense total `71.745,01 €` (white text)
- **Right card** (very light teal/white fill): `"Ingresos"` label + income total `59.184,37 €` (green text)

The left (Gastos) card appears "selected" / more prominent when on the expenses view.

#### 3.8.2 Bar Chart

A horizontal scrollable **bar chart** filling the full width:

- **X-axis**: Years + start dates (e.g. `2022`, `2023`, `2024`, `2025`, `2026`)
- **Y-axis**: Amount scale on the right (e.g. `2.400 €`, `4.800 €`)
- **Bars**: Multi-coloured stacked bars (colours matching category colours) for expenses; single grey bars for income visible behind; bars clustered per month
- **Time span shown**: From `1 ene. 2022` to `31 jul. 2026` — the full "Siempre" (always) period
- When Gastos is selected, bars are multi-colour (category breakdown); when Ingresos is selected, bars turn blue
- A faint pink bar in the current/future month indicates the current period

#### 3.8.3 Averages Row

Three equal-width cells below the chart:

| Cell | Label | Value |
|------|-------|-------|
| 1 | Día (prom.) | `46,95 €` |
| 2 | Semana (prom.) | `328,65 €` |
| 3 | Mes (prom.) | `1.428,13 €` |

- Pink text for averages (expense mode)
- Very light pink background per cell

#### 3.8.4 Category Breakdown List

Below the averages row — a vertical list of categories ranked by spend:

Each row:
```
[ Category icon circle ]  Category name           Amount €
                          [████████░░░░░░░░░░]  XX%
```

- Icon circle on the left (same coloured circles as everywhere else)
- Name + amount right of icon
- Progress bar below (filled portion matches category colour, shows percentage of total)
- Percentage label at the end of the filled portion

Visible categories in order (expenses, all-time):
1. Hogar — `15.509,47 €` — 22%
2. Comestibles — `11.942,04 €` — 17%
3. Coche — `10.103,39 €` — 14%
4. Viajes — `7.052,26 €` — 10%
5. Restaurantes — `4.580,31 €` — 6%
6. Tabaco — `4.127,65 €` — 6%
7. *(more items below)*

A `"Más…"` expandable row at the bottom when collapsed:
- Shows `34.190,11 €` — 48% — with a `⌄` icon

---

## 4. UI Patterns & Components

### 4.1 Bottom Sheet (Modal)
Used extensively for pickers and context menus. Consistent style:
- White rounded card sliding up from bottom
- Dimmed overlay behind
- Title row with icon + label
- Content (radio list, grid, or free form)
- Optional OK/Done button at bottom-right (lavender rounded pill)

### 4.2 Section Headings
Blue (`accent colour`) text labels that divide settings/list pages into semantic groups.
No background, no border — just a colour-change label.

### 4.3 Coloured Account/Category Icons
All entity icons follow a consistent pattern:
- Large filled square (accounts) or circle (categories) with rounded corners
- Solid background colour
- White icon/glyph inside
- Name label above, amount below (in category grid)
- This pattern unifies the visual identity across ALL screens

### 4.4 Context Overflow Menu (`⋮`)
Small popup dropdown card anchored to the trigger button. Plain white card, rounded corners, no icons — just text items. Used for subcategory actions.

### 4.5 Toggle Switches
Standard Android toggle switches used consistently for binary settings.

### 4.6 Radio List Dialogs
Used for: currency selection, currency format, start screen.
- Radio button on left, label on right
- Selected row: blue text + filled blue radio
- Confirmed with an OK button

### 4.7 Two-Column Action Grids
Used in the Account action bottom sheet.
- 2×3 grid of circular icon buttons with labels below
- Each button has a unique accent colour

### 4.8 Collapsible Sections
The "scheduled transactions" banner demonstrates a collapsible/expandable inline section within a list view, with a caret toggle.

### 4.9 Period Navigation Row
Consistent across 3 of 4 main screens. A simple three-zone horizontal row with `<<` / pill / `>>`. The pill shows the current period name and opens the period modal on tap.

### 4.10 Net Balance in Header
The global total balance displayed in the shared header creates persistent context — the user always knows their net position regardless of which tab they're on.

---

## 5. Color & Icon System

### Category Color Assignments (observed)

| Category | Colour (approx) | Notes |
|----------|----------------|-------|
| Comestibles (Groceries) | `#2196F3` blue | Basket icon |
| Restaurantes | `#1A237E` dark navy | Fork + knife |
| Tiempo libre | `#E91E8C` hot pink | Ticket icon |
| Transporte | `#FF8F00` amber/orange | Bus |
| Salud | `#388E3C` green | Heart-hands |
| Regalos | `#D32F2F` red | Gift box |
| Familia | `#7B1FA2` purple | Smiley |
| Compras | `#F57C00` orange | Shopping bag |
| Trabajo | `#283593` indigo | Buildings |
| Hogar | `#F9A825` yellow/gold | House |
| Tabaco | `#4E342E` dark brown | Cigarette |
| Coche | `#9E9E9E` grey | Car |
| Teléfono | `#00897B` teal | Phone |
| Viajes | `#7CB342` lime green | Boat/ship |
| Gobierno | `#212121` near-black | Building |

### Account Icon Examples

| Account | Icon | Background |
|---------|------|-----------|
| Cristina | Smiley emoji | Blue |
| Rafa | Wallet | Teal/green |
| Común (shared) | Credit card | Pink/red |

### Income vs. Expense Colour Convention

| Concept | Colour |
|---------|--------|
| Expense / negative balance | Pink/magenta `~#E91E63` |
| Income / positive balance | Teal/green `~#00897B` |
| Neutral / label | Dark grey `~#424242` |
| Interactive / accent | Indigo blue `~#3F51B5` |
| Destructive actions | Red `~#F44336` |
| Section headings | Accent blue (same as interactive) |

---

## 6. Key UX Decisions Worth Adopting in Kakeibo

### 6.1 Persistent Net Balance in Header
**What**: The total net balance is always visible in the top-centre of every screen.
**Why adopt**: Provides constant financial context without switching screens. The user always knows where they stand.

### 6.2 Category Circle + Donut Chart Layout
**What**: Categories are displayed as coloured circles arranged around a central donut chart. Each circle colour matches a chart segment.
**Why adopt**: Immediately communicates proportional spending by category. Visually rich and informative without tables.

### 6.3 Consistent Colour-Coded Entity Icons
**What**: All accounts and categories use a solid coloured square/circle with a white glyph. Same icon appears on every screen (category list, transaction row, chart segment, breakdown list).
**Why adopt**: Creates a strong visual identity. Users learn their categories by colour, not just name. Reduces cognitive load when scanning transaction lists.

### 6.4 Period Selector with `«« pill »»`
**What**: A 3-zone period row on every analytics screen with fast prev/next navigation and a modal for full period selection.
**Why adopt**: Fast temporal navigation is essential for finance apps. The `«»` arrows allow quick day/week/month stepping without opening a modal.

### 6.5 Account Context Actions as Bottom Sheet Grid
**What**: Long-pressing an account reveals a bottom sheet with a 2×3 grid of quick actions (Edit, Balance, Transactions, Deposit, Withdraw, Transfer).
**Why adopt**: Exposes account-level actions without cluttering the list view. Grid layout is more scannable than a vertical menu for 6 actions.

### 6.6 Account Type Picker on Creation
**What**: When creating a new account, a bottom sheet presents 3 account types (Regular, Debt, Savings) with illustrations and subtitles before showing the form.
**Why adopt**: Prevents the user from having to understand technical type settings inside the form. The illustrated picker is intuitive.

### 6.7 Transaction List Grouped by Date
**What**: Transactions are grouped under large date headers (big day number + day name + month/year). Each group shows a subtotal on the right.
**Why adopt**: Date grouping is the natural mental model for reviewing spending. The large day number is a strong visual anchor.

### 6.8 Scheduled Transactions Banner
**What**: An inline collapsible banner at the top of the transaction list shows upcoming/scheduled transactions.
**Why adopt**: Keeps future transactions visible without a separate screen, while keeping the banner out of the way when not needed.

### 6.9 Summary Averages Row
**What**: Below the bar chart, three cards show daily / weekly / monthly averages for the selected period.
**Why adopt**: Averages are more actionable than totals — "I spend €46/day" is more relatable than "€71k total".

### 6.10 Category Breakdown with Percentage Progress Bars
**What**: Below the chart, categories are ranked with a coloured progress bar and percentage.
**Why adopt**: Instantly shows relative weight of each spending category. More readable than a pie chart legend or a raw number table.

### 6.11 Subcategories
**What**: Each category can have subcategories with their own icons. Subcategories can be promoted to categories or merged.
**Why adopt**: Allows granular tracking (e.g., "Comestibles > Pescadería / Frutería") without cluttering the top-level category list.

### 6.12 Themed Accent Colour (User-selectable)
**What**: Users can pick from 15 accent colours that propagate across the entire UI.
**Why adopt**: Personalisation increases ownership and engagement. Technically simple if CSS variables or a theme token system is used.

### 6.13 "My Finances" Net Worth View
**What**: The Accounts screen has a second tab showing a simple ASSETS vs. DEBTS table with a net total.
**Why adopt**: A one-glance net worth view is a natural companion to the account list.

---

*Study created on 2026-03-08. Based on 23 screenshots of the OneMoney Android app.*
