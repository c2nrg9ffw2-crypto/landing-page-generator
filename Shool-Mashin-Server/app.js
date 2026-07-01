const STORAGE_KEY = 'vendingMachineData';
const HISTORY_KEY = 'vendingMachineHistory';

let overrideDate = localStorage.getItem('overrideDate') || '';
let hiddenSnacks = new Set(JSON.parse(localStorage.getItem('hiddenSnacks') || '[]'));

function migrateData() {
  const data = JSON.parse(localStorage.getItem(STORAGE_KEY) || '[]');
  let changed = false;
  data.forEach(item => {
    if (item.sellPrice != null && item.margin == null) {
      if (item.buyCost != null && item.buyCost > 0)
        item.margin = Math.round(((item.sellPrice / item.buyCost) - 1) * 10000) / 100;
      delete item.sellPrice;
      changed = true;
    }
  });
  if (changed) localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
}
migrateData();

function computeSellPrice(item) {
  if (item.buyCost == null || item.margin == null) return null;
  return item.buyCost * (1 + item.margin / 100);
}

function getLogDate() {
  if (overrideDate) return overrideDate;
  const yesterday = new Date();
  yesterday.setDate(yesterday.getDate() - 1);
  return yesterday.toISOString().slice(0, 10);
}

function updateClock() {
  const el = document.getElementById('currentDateTime');
  if (!el) return;
  const now = new Date();
  el.textContent = now.toLocaleString('en-GB', {
    weekday: 'short', day: '2-digit', month: 'short', year: 'numeric',
    hour: '2-digit', minute: '2-digit', second: '2-digit'
  });
}
setInterval(updateClock, 1000);
updateClock();

function loadHistory() {
  return JSON.parse(localStorage.getItem(HISTORY_KEY) || '[]');
}

function saveHistory(history) {
  localStorage.setItem(HISTORY_KEY, JSON.stringify(history));
}

function load() {
  return JSON.parse(localStorage.getItem(STORAGE_KEY) || '[]');
}

function save(data) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
}

function statusLabel(remaining, starting) {
  const pct = remaining / starting;
  if (remaining === 0) return { text: 'Empty', cls: 'status-empty' };
  if (pct <= 0.25) return { text: 'Low', cls: 'status-low' };
  return { text: 'OK', cls: 'status-ok' };
}

function updateProfitSummary(data) {
  let revenue = 0, cost = 0, hasAny = false;
  data.forEach(item => {
    const sp = computeSellPrice(item);
    if (sp != null) { revenue += sp * item.totalPurchased; hasAny = true; }
    if (item.buyCost != null) { cost += item.buyCost * item.startingStock; hasAny = true; }
  });
  const profit = revenue - cost;
  const profitEl = document.getElementById('statProfit');
  document.getElementById('statRevenue').textContent = hasAny ? fmt(revenue) : '—';
  document.getElementById('statCost').textContent    = hasAny ? fmt(cost)    : '—';
  profitEl.textContent = hasAny ? fmt(profit) : '—';
  profitEl.className = 'profit-value ' + (hasAny ? (profit >= 0 ? 'profit-pos' : 'profit-neg') : '');
}

function render() {
  const data = load();
  const tbody = document.getElementById('tableBody');
  const saleSelect = document.getElementById('saleSnack');
  const restockSelect = document.getElementById('restockSnack');

  tbody.innerHTML = '';
  saleSelect.innerHTML = '<option value="">— Select snack —</option>';
  restockSelect.innerHTML = '<option value="">— Select snack —</option>';

  if (data.length === 0) {
    tbody.innerHTML = '<tr id="emptyRow"><td colspan="9" class="empty">No snacks added yet.</td></tr>';
    updateProfitSummary([]);
    return;
  }

  data.forEach(item => {
    const { text, cls } = statusLabel(item.remaining, item.startingStock);

    const tr = document.createElement('tr');
    const sellPrice = computeSellPrice(item);
    const itemProfit = (item.buyCost != null && sellPrice != null)
      ? (sellPrice * item.totalPurchased) - (item.buyCost * item.startingStock)
      : null;
    const profitCls = itemProfit == null ? '' : itemProfit >= 0 ? 'profit-pos' : 'profit-neg';

    tr.innerHTML = `
      <td>${escapeHtml(item.name)}</td>
      <td>${fmt(item.buyCost)}</td>
      <td>${sellPrice != null ? fmt(sellPrice) : '—'}</td>
      <td>${item.margin != null ? item.margin.toFixed(1) + '%' : '—'}</td>
      <td>${item.totalPurchased}</td>
      <td><strong>${item.remaining}</strong></td>
      <td class="${profitCls}">${itemProfit != null ? fmt(itemProfit) : '—'}</td>
      <td><span class="badge ${cls}">${text}</span></td>
      <td><button class="del-btn" data-id="${item.id}">✕</button></td>
    `;
    tbody.appendChild(tr);

    [saleSelect, restockSelect].forEach(sel => {
      const opt = document.createElement('option');
      opt.value = item.id;
      opt.textContent = `${item.name} (${item.remaining} left)`;
      sel.appendChild(opt);
    });
  });

  document.querySelectorAll('.del-btn').forEach(btn => {
    btn.addEventListener('click', () => deleteSnack(Number(btn.dataset.id)));
  });

  updateProfitSummary(data);
  renderSnackToggles();
  renderEditPrices();
}

function renderEditPrices() {
  const data = load();
  const container = document.getElementById('editPrices');
  container.innerHTML = '';

  if (data.length === 0) {
    container.innerHTML = '<p class="settings-hint">No snacks yet.</p>';
    return;
  }

  data.forEach(item => {
    const row = document.createElement('div');
    row.className = 'price-edit-row';

    const nameSpan = document.createElement('span');
    nameSpan.className = 'price-edit-name';
    nameSpan.textContent = item.name;

    ['buyCost', 'margin'].forEach(field => {
      const input = document.createElement('input');
      input.type = 'number';
      input.className = 'price-input';
      input.placeholder = field === 'buyCost' ? 'Buy' : 'Margin%';
      input.min = field === 'buyCost' ? 0 : -100;
      input.step = field === 'buyCost' ? '0.01' : '0.1';
      if (item[field] != null) input.value = item[field];
      input.addEventListener('change', () => {
        const d = load();
        const target = d.find(s => s.id === item.id);
        if (!target) return;
        target[field] = input.value !== '' ? parseFloat(input.value) : null;
        save(d);
        render();
      });
      row.appendChild(input);
    });

    row.insertBefore(nameSpan, row.firstChild);
    container.appendChild(row);
  });
}

function renderSnackToggles() {
  const data = load();
  const container = document.getElementById('snackToggles');
  container.innerHTML = '';
  data.forEach(item => {
    const label = document.createElement('label');
    label.className = 'toggle-label snack-toggle';

    const span = document.createElement('span');
    span.textContent = item.name;

    const leverEl = document.createElement('span');
    leverEl.className = 'lever';

    const cb = document.createElement('input');
    cb.type = 'checkbox';
    cb.checked = !hiddenSnacks.has(item.name);
    cb.addEventListener('change', () => {
      if (cb.checked) hiddenSnacks.delete(item.name);
      else hiddenSnacks.add(item.name);
      localStorage.setItem('hiddenSnacks', JSON.stringify([...hiddenSnacks]));
      renderChart();
    });

    const sliderSpan = document.createElement('span');
    sliderSpan.className = 'lever-slider';

    leverEl.appendChild(cb);
    leverEl.appendChild(sliderSpan);
    label.appendChild(span);
    label.appendChild(leverEl);
    container.appendChild(label);
  });
}

function fmt(val) {
  return val != null ? Number(val).toFixed(2) : '—';
}

function escapeHtml(str) {
  return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

function addSnack() {
  const nameEl = document.getElementById('snackName');
  const stockEl = document.getElementById('snackStock');
  const buyCostEl = document.getElementById('snackBuyCost');
  const marginEl = document.getElementById('snackSellPrice');
  const errEl = document.getElementById('addError');

  const name = nameEl.value.trim();
  const stock = parseInt(stockEl.value, 10);
  const buyCost = buyCostEl.value !== '' ? parseFloat(buyCostEl.value) : null;
  const margin = marginEl.value !== '' ? parseFloat(marginEl.value) : null;

  if (!name) { errEl.textContent = 'Please enter a snack name.'; return; }
  if (!stock || stock < 1) { errEl.textContent = 'Starting stock must be at least 1.'; return; }

  const data = load();
  if (data.some(s => s.name.toLowerCase() === name.toLowerCase())) {
    errEl.textContent = 'A snack with that name already exists.';
    return;
  }

  const id = data.length ? Math.max(...data.map(s => s.id)) + 1 : 1;
  data.push({ id, name, startingStock: stock, totalPurchased: 0, remaining: stock, buyCost, margin });
  save(data);

  nameEl.value = '';
  stockEl.value = '';
  buyCostEl.value = '';
  marginEl.value = '';
  errEl.textContent = '';
  render();
}

function logSale() {
  const selectEl = document.getElementById('saleSnack');
  const qtyEl = document.getElementById('saleQty');
  const errEl = document.getElementById('saleError');

  const id = Number(selectEl.value);
  const qty = parseInt(qtyEl.value, 10);

  if (!id) { errEl.textContent = 'Please select a snack.'; return; }
  if (!qty || qty < 1) { errEl.textContent = 'Quantity must be at least 1.'; return; }

  const data = load();
  const item = data.find(s => s.id === id);

  if (qty > item.remaining) {
    errEl.textContent = `Not enough stock — only ${item.remaining} remaining.`;
    return;
  }

  item.totalPurchased += qty;
  item.remaining -= qty;
  save(data);

  const history = loadHistory();
  history.push({ date: getLogDate(), snackName: item.name, qty });
  saveHistory(history);

  qtyEl.value = '';
  errEl.textContent = '';
  render();
  renderChart();
}

function restock() {
  const selectEl = document.getElementById('restockSnack');
  const qtyEl = document.getElementById('restockQty');
  const errEl = document.getElementById('restockError');

  const id = Number(selectEl.value);
  const qty = parseInt(qtyEl.value, 10);

  if (!id) { errEl.textContent = 'Please select a snack.'; return; }
  if (!qty || qty < 1) { errEl.textContent = 'Quantity must be at least 1.'; return; }

  const data = load();
  const item = data.find(s => s.id === id);

  item.startingStock += qty;
  item.remaining += qty;
  save(data);

  qtyEl.value = '';
  errEl.textContent = '';
  render();
}

function deleteSnack(id) {
  const data = load();
  const removed = data.find(s => s.id === id);
  if (removed) {
    hiddenSnacks.delete(removed.name);
    localStorage.setItem('hiddenSnacks', JSON.stringify([...hiddenSnacks]));
  }
  save(data.filter(s => s.id !== id));
  render();
  renderChart();
}

let chartInstance = null;
let chartView = 'daily';

const CHART_COLORS = ['#4299e1','#48bb78','#ed8936','#9f7aea','#f56565','#38b2ac','#f6ad55','#76e4f7'];

function renderChart() {
  const history = loadHistory();

  const snacks = [...new Set(history.map(h => h.snackName))].sort();
  const allKeys = [...new Set(history.map(h =>
    chartView === 'daily' ? h.date : h.date.slice(0, 7)
  ))].sort();

  const bySnack = {};
  history.forEach(({ date, snackName, qty }) => {
    const key = chartView === 'daily' ? date : date.slice(0, 7);
    if (!bySnack[snackName]) bySnack[snackName] = {};
    bySnack[snackName][key] = (bySnack[snackName][key] || 0) + qty;
  });

  const datasets = snacks.filter(s => !hiddenSnacks.has(s)).map(snack => {
    const colorIdx = snacks.indexOf(snack) % CHART_COLORS.length;
    return {
    label: snack,
    data: allKeys.map(k => bySnack[snack]?.[k] || 0),
    borderColor: CHART_COLORS[colorIdx],
    backgroundColor: CHART_COLORS[colorIdx] + '22',
    borderWidth: 2,
    pointRadius: 3,
    tension: 0.3,
    fill: false,
  };});

  const ctx = document.getElementById('purchaseChart').getContext('2d');
  if (chartInstance) chartInstance.destroy();

  chartInstance = new Chart(ctx, {
    type: 'line',
    data: { labels: allKeys, datasets },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: true,
          position: 'bottom',
          labels: { boxWidth: 10, padding: 8, font: { size: 10 } }
        }
      },
      scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } }
    }
  });

  document.getElementById('viewDaily').classList.toggle('active', chartView === 'daily');
  document.getElementById('viewMonthly').classList.toggle('active', chartView === 'monthly');
}

function exportToExcel() {
  const data = load();
  const history = loadHistory();

  // Sheet 1: All sales (raw log)
  const snackMap = Object.fromEntries(data.map(s => [s.name, s]));
  const historyRows = [['Date', 'Snack', 'Quantity', 'Profit']];
  [...history].sort((a, b) => a.date.localeCompare(b.date))
    .forEach(h => {
      const item = snackMap[h.snackName];
      const sp = item ? computeSellPrice(item) : null;
      const profit = (item?.buyCost != null && sp != null)
        ? Math.round((sp - item.buyCost) * h.qty * 100) / 100
        : '';
      historyRows.push([h.date, h.snackName, h.qty, profit]);
    });

  // Sheet 3: Daily totals — one row per day, one column per snack
  const snackNames = [...new Set(history.map(h => h.snackName))].sort();
  const byDay = {};
  history.forEach(({ date, snackName, qty }) => {
    if (!byDay[date]) byDay[date] = {};
    byDay[date][snackName] = (byDay[date][snackName] || 0) + qty;
  });
  const dailyRows = [['Date', ...snackNames, 'Total']];
  Object.keys(byDay).sort().forEach(date => {
    const row = [date, ...snackNames.map(s => byDay[date][s] || 0)];
    row.push(snackNames.reduce((sum, s) => sum + (byDay[date][s] || 0), 0));
    dailyRows.push(row);
  });

  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(historyRows), 'Sales Log');
  XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(dailyRows), 'Daily Totals');
  XLSX.writeFile(wb, `vending-machine-${new Date().toISOString().slice(0,10)}.xlsx`);
}

document.getElementById('exportBtn').addEventListener('click', exportToExcel);
document.getElementById('addSnackBtn').addEventListener('click', addSnack);
document.getElementById('logSaleBtn').addEventListener('click', logSale);
document.getElementById('restockBtn').addEventListener('click', restock);

document.getElementById('viewDaily').addEventListener('click', () => { chartView = 'daily'; renderChart(); });
document.getElementById('viewMonthly').addEventListener('click', () => { chartView = 'monthly'; renderChart(); });

// Settings panel
const settingsPanel = document.getElementById('settingsPanel');
const settingsBackdrop = document.getElementById('settingsBackdrop');

function openSettings() {
  settingsPanel.classList.add('open');
  settingsBackdrop.classList.add('open');
}

function closeSettings() {
  settingsPanel.classList.remove('open');
  settingsBackdrop.classList.remove('open');
}

document.getElementById('settingsBtn').addEventListener('click', openSettings);
document.getElementById('closeSettings').addEventListener('click', closeSettings);
settingsBackdrop.addEventListener('click', closeSettings);

// Profit summary toggle
const toggleProfit = document.getElementById('toggleProfit');
const profitSummary = document.querySelector('.profit-summary');
toggleProfit.checked = localStorage.getItem('showProfit') !== 'false';
profitSummary.style.display = toggleProfit.checked ? '' : 'none';
toggleProfit.addEventListener('change', () => {
  localStorage.setItem('showProfit', toggleProfit.checked);
  profitSummary.style.display = toggleProfit.checked ? '' : 'none';
});

// Bulk price adjustment
function applyPriceChange(fields, confirmMsg) {
  const pct = parseFloat(document.getElementById('priceChangePct').value);
  if (!pct) return;
  if (confirmMsg && !window.confirm(confirmMsg)) return;
  const multiplier = 1 + pct / 100;
  const data = load();
  data.forEach(item => {
    fields.forEach(f => {
      if (item[f] != null) item[f] = Math.round(item[f] * multiplier * 100) / 100;
    });
  });
  save(data);
  render();
}

document.getElementById('applySellPrice').addEventListener('click', () =>
  applyPriceChange(['margin']));

// Dark mode
const toggleDark = document.getElementById('toggleDark');
function applyDark(on) { document.body.classList.toggle('dark', on); }
toggleDark.checked = localStorage.getItem('darkMode') === 'true';
applyDark(toggleDark.checked);
toggleDark.addEventListener('change', () => {
  localStorage.setItem('darkMode', toggleDark.checked);
  applyDark(toggleDark.checked);
});

// Override date
const overrideDateInput = document.getElementById('overrideDate');
overrideDateInput.value = overrideDate;
overrideDateInput.addEventListener('change', () => {
  overrideDate = overrideDateInput.value;
  localStorage.setItem('overrideDate', overrideDate);
});
document.getElementById('resetDate').addEventListener('click', () => {
  overrideDate = '';
  overrideDateInput.value = '';
  localStorage.removeItem('overrideDate');
});

// Chart toggle
const toggleChart = document.getElementById('toggleChart');
const chartCard = document.querySelector('.chart-card');
const snackTogglesSection = document.getElementById('snackTogglesSection');

function applyChartVisibility(show) {
  chartCard.style.display = show ? '' : 'none';
  snackTogglesSection.classList.toggle('visible', show);
}

toggleChart.checked = localStorage.getItem('showChart') !== 'false';
applyChartVisibility(toggleChart.checked);

toggleChart.addEventListener('change', () => {
  const show = toggleChart.checked;
  localStorage.setItem('showChart', show);
  applyChartVisibility(show);
  if (show) renderChart();
});

render();
renderChart();
