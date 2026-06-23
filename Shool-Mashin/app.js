const STORAGE_KEY = 'vendingMachineData';

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

function render() {
  const data = load();
  const tbody = document.getElementById('tableBody');
  const saleSelect = document.getElementById('saleSnack');
  const restockSelect = document.getElementById('restockSnack');

  tbody.innerHTML = '';
  saleSelect.innerHTML = '<option value="">— Select snack —</option>';
  restockSelect.innerHTML = '<option value="">— Select snack —</option>';

  if (data.length === 0) {
    tbody.innerHTML = '<tr id="emptyRow"><td colspan="6" class="empty">No snacks added yet.</td></tr>';
    return;
  }

  data.forEach(item => {
    const { text, cls } = statusLabel(item.remaining, item.startingStock);

    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td>${item.name}</td>
      <td>${item.startingStock}</td>
      <td>${item.totalPurchased}</td>
      <td><strong>${item.remaining}</strong></td>
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
}

function addSnack() {
  const nameEl = document.getElementById('snackName');
  const stockEl = document.getElementById('snackStock');
  const errEl = document.getElementById('addError');

  const name = nameEl.value.trim();
  const stock = parseInt(stockEl.value, 10);

  if (!name) { errEl.textContent = 'Please enter a snack name.'; return; }
  if (!stock || stock < 1) { errEl.textContent = 'Starting stock must be at least 1.'; return; }

  const data = load();
  if (data.some(s => s.name.toLowerCase() === name.toLowerCase())) {
    errEl.textContent = 'A snack with that name already exists.';
    return;
  }

  const id = data.length ? Math.max(...data.map(s => s.id)) + 1 : 1;
  data.push({ id, name, startingStock: stock, totalPurchased: 0, remaining: stock });
  save(data);

  nameEl.value = '';
  stockEl.value = '';
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

  qtyEl.value = '';
  errEl.textContent = '';
  render();
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
  const data = load().filter(s => s.id !== id);
  save(data);
  render();
}

document.getElementById('addSnackBtn').addEventListener('click', addSnack);
document.getElementById('logSaleBtn').addEventListener('click', logSale);
document.getElementById('restockBtn').addEventListener('click', restock);

render();
