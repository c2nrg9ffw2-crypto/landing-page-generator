# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Running the App

No build step. Two ways to run:

**Direct file open (Windows):**
```
start index.html
```

**macOS server (use `Shool-Mashin-Server/` folder instead):**
```bash
npm install
npm start
# opens at http://localhost:8080
```

**Any machine (Python, pre-installed on macOS):**
```bash
python3 -m http.server 8080
```

After any edit, reopen or hard-refresh the browser page to see changes.

External dependencies loaded from CDN (internet required):
- Chart.js — `cdn.jsdelivr.net`
- SheetJS (xlsx) — `unpkg.com`

## Architecture

Three files, no framework, no build tool:

- **`index.html`** — markup only; no inline scripts or styles
- **`app.js`** — all logic; plain ES6, no modules
- **`style.css`** — CSS Grid (`.panels`, `.profit-summary`), Flexbox (`.form-row`, `.settings-panel`)

A sister folder **`Shool-Mashin-Server/`** contains the same three files plus `server.js` (Express) and `package.json` for macOS server deployment.

## Data Layer

Two `localStorage` keys:

| Key | Shape | Purpose |
|---|---|---|
| `vendingMachineData` | `SnackItem[]` | Current inventory |
| `vendingMachineHistory` | `HistoryEntry[]` | Per-sale log for chart + export |

```js
// SnackItem
{ id, name, startingStock, totalPurchased, remaining, buyCost, margin }

// HistoryEntry
{ date: "YYYY-MM-DD", snackName: string, qty: number }
```

`remaining` is stored and kept in sync manually — not computed on read.
`sellPrice` is always **computed**, never stored (see Pricing Model).
On load, `migrateData()` converts any legacy `sellPrice` field to `margin`.

Additional `localStorage` keys for UI state:
- `showChart`, `showProfit`, `darkMode` — boolean toggles
- `hiddenSnacks` — JSON array of snack names hidden from the chart
- `overrideDate` — ISO date string used instead of today when logging sales

`chartView` (`'daily'` | `'monthly'`) is a module-level variable — **not** persisted to localStorage; resets to `'daily'` on every page reload.

## Render Cycle

Every mutation ends with `render()`, which rebuilds the entire table body, both `<select>` dropdowns, the snack chart toggles, and the edit-prices list from scratch. `renderChart()` is called separately and destroys/recreates the Chart.js instance each time.

## Gotchas

- `#snackSellPrice` in the HTML captures **margin %**, not sell price — the ID is a naming artifact from before the margin migration.
- The "Add Snack" form lives inside the Settings slide-in panel (`#settingsPanel`), not in the main page body.

## Key Features & Where They Live

| Feature | Location |
|---|---|
| Settings slide-in panel | `#settingsPanel`, toggled via `openSettings()` / `closeSettings()` |
| Dark mode | `body.dark` CSS class; toggled by `applyDark()` |
| Profit summary cards | `.profit-summary`; updated by `updateProfitSummary(data)` |
| Per-snack chart toggles | `renderSnackToggles()` → `hiddenSnacks` Set |
| Bulk margin adjustment | `applyPriceChange(['margin'])` |
| Sale date override | `getLogDate()` returns `overrideDate` or today |
| Export to .xlsx | `exportToExcel()` using SheetJS — Sales Log (per-entry: date, snack, qty, profit) and Daily Totals (per-day pivot: snack qty columns + Total + Profit) |
| Reset all data | Red button at bottom of Settings panel — `localStorage.clear()` + `location.reload()` |

## Pricing Model

- `buyCost` — unit cost to stock the item
- `margin` — profit margin % entered by the user
- `sellPrice` — computed as `buyCost × (1 + margin / 100)` via `computeSellPrice(item)`
- **Cost** in profit summary = `buyCost × startingStock` (all stocked units, not just sold). `restock()` adds to `startingStock` cumulatively, which raises this cost figure permanently.
- **Revenue** = `sellPrice × totalPurchased`
- **Profit** = Revenue − Cost
- All monetary values displayed with `fmt()`, which appends ` kr` (SEK)

## Status Badge Logic (`statusLabel`)

| Condition | Badge |
|---|---|
| `remaining === 0` | Empty (red) |
| `remaining / startingStock ≤ 0.25` | Low (yellow) |
| otherwise | OK (green) |
