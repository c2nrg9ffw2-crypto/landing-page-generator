# Vending Machine Tracker

## Project Goal

Build a web-based vending machine inventory tracker. The app lets staff log snack purchases, see how many of each item have been sold, and check how many remain in stock.

## Features to Build

1. **Inventory display** — show each snack with its name, total purchased, and remaining stock.
2. **Data input form** — a form where the user can:
   - Add a new snack (name + starting stock quantity).
   - Record a purchase (select snack, enter quantity bought).
3. **Live totals** — after each purchase entry, remaining stock updates automatically (starting stock − total purchased).
4. **Persist data** — save inventory to a local JSON file or browser localStorage so data survives a page refresh.

## Tech Stack

- **Frontend:** Plain HTML + CSS + JavaScript (no framework needed) OR React if you prefer.
- **Backend (optional):** A small Node.js/Express server with a JSON file as the database if you want server-side persistence.
- **No external databases required** — keep it simple.

## Data Model

Each snack entry looks like this:

```json
{
  "id": 1,
  "name": "Chips",
  "startingStock": 20,
  "totalPurchased": 7,
  "remaining": 13
}
```

`remaining` is always computed as `startingStock - totalPurchased`.

## UI Layout

```
┌─────────────────────────────────────────────┐
│         Vending Machine Tracker             │
├─────────────────────────────────────────────┤
│  [Add Snack]  Name: ______  Stock: __  [+]  │
│  [Log Sale]   Snack: v____  Qty:   __  [✓]  │
├─────────────────────────────────────────────┤
│  Snack      | Purchased | Remaining         │
│  ---------- | --------- | ---------         │
│  Chips      |     7     |    13             │
│  Candy Bar  |     3     |    17             │
│  Water      |    12     |     8             │
└─────────────────────────────────────────────┘
```

## Rules

- Remaining stock must never go below 0; show a warning if a sale would exceed stock.
- All snack names must be unique.
- Quantities must be positive integers.
- Keep the code in a single `index.html` + `app.js` + `style.css` structure unless a backend is added.
