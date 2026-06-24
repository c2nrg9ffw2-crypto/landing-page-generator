# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Running the App

No build step. Serve the directory with any static file server — opening `index.html` directly as a `file://` URL also works but some browsers restrict it.

**macOS (Python, pre-installed):**
```bash
python3 -m http.server 8080
# open http://localhost:8080
```

**macOS/Windows (Node.js):**
```bash
npx serve .
```

**Windows (file open):**
```
start index.html
```

Chart.js is loaded from CDN (`cdn.jsdelivr.net`), so an internet connection is required for the chart to render.

After any edit, reopen or hard-refresh the browser page to see changes.

## Architecture

Three files, no framework, no dependencies to install:

- **`index.html`** — markup only; no inline scripts or styles
- **`app.js`** — all logic; plain ES6, no modules
- **`style.css`** — layout uses CSS Grid (`.panels`) and Flexbox (`.form-row`)

### Data Layer

Everything is persisted to `localStorage` under two keys:

| Key | Shape | Purpose |
|---|---|---|
| `vendingMachineData` | `SnackItem[]` | Current inventory |
| `vendingMachineHistory` | `HistoryEntry[]` | Per-sale log for the chart |

```js
// SnackItem
{ id: number, name: string, startingStock: number, totalPurchased: number, remaining: number }

// HistoryEntry
{ date: "YYYY-MM-DD", snackName: string, qty: number }
```

`remaining` is stored alongside `startingStock` and `totalPurchased` — it is kept in sync manually, not computed on read. `restock` increments both `startingStock` and `remaining` so the Low/Empty status thresholds scale correctly.

### Render Cycle

Every mutation (`addSnack`, `logSale`, `restock`, `deleteSnack`) ends with `render()`. `render()` rebuilds the entire table body and repopulates both `<select>` dropdowns from scratch on each call. `renderChart()` is called separately and rebuilds the Chart.js instance (destroying the previous one via `chartInstance.destroy()`).

### Status Badge Logic (`statusLabel`)

| Condition | Badge |
|---|---|
| `remaining === 0` | Empty (red) |
| `remaining / startingStock ≤ 0.25` | Low (yellow) |
| otherwise | OK (green) |

### ID Generation

`id` is `Math.max(...data.map(s => s.id)) + 1`, or `1` if the array is empty. IDs are never reused after deletion.
