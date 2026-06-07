// ─── Config ──────────────────────────────────────────────────────────────────
const API = '';  // same origin
const token = () => localStorage.getItem('token');

// ─── City Autocomplete Engine ─────────────────────────────────────────────────
let _allCities = [];  // cached from DB

async function loadCities() {
  try {
    const res  = await fetch('/api/ai/cities');
    const data = await res.json();
    if (data.success) _allCities = data.data;
  } catch { /* silently ignore */ }
}

/**
 * Attach autocomplete to an <input> inside a .autocomplete-wrap element.
 * @param {HTMLInputElement} input
 */
function attachAutocomplete(input) {
  const wrap = input.closest('.autocomplete-wrap');
  if (!wrap) return;

  let ddEl    = null;   // dropdown div
  let selIdx  = -1;     // keyboard cursor

  function getItems() {
    const q = input.value.trim().toLowerCase();
    if (!q) return _allCities;
    return _allCities.filter(c => c.toLowerCase().startsWith(q));
  }

  function closeDropdown() {
    if (ddEl) { ddEl.remove(); ddEl = null; selIdx = -1; }
  }

  function highlight(idx) {
    const items = ddEl?.querySelectorAll('.ac-item');
    if (!items) return;
    items.forEach((el, i) => el.classList.toggle('selected', i === idx));
    if (items[idx]) items[idx].scrollIntoView({ block: 'nearest' });
  }

  function openDropdown() {
    closeDropdown();
    const matches = getItems();

    ddEl = document.createElement('div');
    ddEl.className = 'autocomplete-dropdown';

    if (!matches.length) {
      ddEl.innerHTML = '<div class="ac-empty">No cities found</div>';
    } else {
      matches.slice(0, 30).forEach((city, i) => {   // max 30 items
        const item = document.createElement('div');
        item.className = 'ac-item';
        item.innerHTML = `<span class="ac-icon">📍</span>${city}`;
        item.addEventListener('mousedown', e => {
          e.preventDefault();
          input.value = city;
          closeDropdown();
          input.dispatchEvent(new Event('change'));
        });
        ddEl.appendChild(item);
      });
    }

    wrap.appendChild(ddEl);
    selIdx = -1;
  }

  input.addEventListener('focus',  () => openDropdown());
  input.addEventListener('input',  () => openDropdown());
  input.addEventListener('blur',   () => setTimeout(closeDropdown, 150));

  input.addEventListener('keydown', e => {
    if (!ddEl) return;
    const items = ddEl.querySelectorAll('.ac-item');
    if (e.key === 'ArrowDown') {
      e.preventDefault(); selIdx = Math.min(selIdx + 1, items.length - 1); highlight(selIdx);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault(); selIdx = Math.max(selIdx - 1, 0); highlight(selIdx);
    } else if (e.key === 'Enter' && selIdx >= 0) {
      e.preventDefault(); items[selIdx].dispatchEvent(new Event('mousedown'));
    } else if (e.key === 'Escape') {
      closeDropdown();
    }
  });
}

// ─── State ───────────────────────────────────────────────────────────────────
let estimateReq = null;
let estimation = null;
let selectedType = null;
let planResult = null;

// ─── Init ─────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  const today = new Date().toISOString().split('T')[0];
  document.getElementById('depDate').min = today;
  document.getElementById('retDate').min = today;
  addCity(); // default one city row
  
  // Load cities then attach autocomplete
  loadCities().then(() => {
    attachAutocomplete(document.getElementById('fromCity'));
    attachAutocomplete(document.getElementById('toCity'));
  });
});

// ─── Navigation ───────────────────────────────────────────────────────────────
function goPage(n) {
  document.querySelectorAll('.page').forEach((p, i) => p.classList.toggle('active', i + 1 === n));
  [1, 2, 3].forEach(i => {
    const s = document.getElementById('s' + i);
    s.classList.remove('active', 'done');
    if (i < n) s.classList.add('done');
    if (i === n) s.classList.add('active');
  });
  window.scrollTo({ top: 0, behavior: 'smooth' });
}

function showAlert(id, msg) {
  const el = document.getElementById(id);
  el.textContent = msg; el.classList.add('show');
  setTimeout(() => el.classList.remove('show'), 5000);
}

// ─── Multi-City ───────────────────────────────────────────────────────────────
function toggleItinerary() {
  const on = document.getElementById('multiCity').checked;
  document.getElementById('itinerarySection').style.display = on ? 'block' : 'none';
  if (on) checkDays();
}

function addCity() {
  const r = document.createElement('div');
  r.className = 'city-row';
  r.innerHTML = `
    <div class="form-group"><label>City</label><div class="autocomplete-wrap"><input type="text" class="city-name" placeholder="e.g. Luxor" autocomplete="off"></div></div>
    <div class="form-group"><label>Days</label><input type="number" class="city-days" min="1" value="3" oninput="checkDays()"></div>
    <button onclick="this.parentElement.remove();checkDays()">✕</button>`;
  document.getElementById('cityRows').appendChild(r);
  // Attach autocomplete after cities are loaded
  const inp = r.querySelector('.city-name');
  if (_allCities.length) attachAutocomplete(inp);
  else loadCities().then(() => attachAutocomplete(inp));
  checkDays();
}

function checkDays() {
  const dep = document.getElementById('depDate').value;
  const ret = document.getElementById('retDate').value;
  const hint = document.getElementById('daysHint');
  if (!dep || !ret) { hint.style.display = 'none'; return; }
  const total = Math.round((new Date(ret) - new Date(dep)) / (864e5));
  const sum = [...document.querySelectorAll('.city-days')].reduce((a, b) => a + parseInt(b.value || 0), 0);
  hint.style.display = 'block';
  hint.className = 'days-hint ' + (sum === total ? 'days-ok' : 'days-err');
  hint.textContent = sum === total
    ? `✅ ${sum} days — matches your trip duration`
    : `⚠️ ${sum} days allocated, trip is ${total} days (difference: ${sum - total > 0 ? '+' : ''}${sum - total})`;
}

document.getElementById('depDate').addEventListener('change', checkDays);
document.getElementById('retDate').addEventListener('change', checkDays);

// ─── Build request object ─────────────────────────────────────────────────────
function buildRequest() {
  const dep = document.getElementById('depDate').value;
  const ret = document.getElementById('retDate').value;
  const multi = document.getElementById('multiCity').checked;
  let itinerary = null;

  if (multi) {
    itinerary = [];
    document.querySelectorAll('.city-row').forEach(row => {
      const city = row.querySelector('.city-name').value.trim();
      const days = parseInt(row.querySelector('.city-days').value) || 0;
      if (city && days > 0) itinerary.push({ city, days });
    });
    if (!itinerary.length) return null;
    const total = Math.round((new Date(ret) - new Date(dep)) / (864e5));
    const sum = itinerary.reduce((a, b) => a + b.days, 0);
    if (sum !== total) return { _err: `Itinerary total (${sum} days) must equal trip duration (${total} days)` };
  }

  return {
    fromCity: document.getElementById('fromCity').value.trim(),
    fromCountry: '',
    toCity: document.getElementById('toCity').value.trim(),
    toCountry: '',
    departureDate: dep,
    returnDate: ret,
    adults: parseInt(document.getElementById('adults').value) || 1,
    children: parseInt(document.getElementById('children').value) || 0,
    singleRooms: parseInt(document.getElementById('singleRooms').value) || 0,
    doubleRooms: parseInt(document.getElementById('doubleRooms').value) || 1,
    touristLanguage: document.getElementById('language').value,
    excludeFlights: !document.getElementById('inclFlights').checked,
    excludeHotels: !document.getElementById('inclHotels').checked,
    excludeTours: !document.getElementById('inclTours').checked,
    itinerary
  };
}

// ─── STEP 1 → 2: Estimate ────────────────────────────────────────────────────
async function goEstimate() {
  const req = buildRequest();
  if (!req) return showAlert('alert1', 'Please fill all required fields.');
  if (req._err) return showAlert('alert1', req._err);
  if (!req.fromCity) return showAlert('alert1', 'From City is required.');
  if (!req.toCity) return showAlert('alert1', 'To City is required.');
  if (!req.departureDate || !req.returnDate) return showAlert('alert1', 'Select travel dates.');
  if (new Date(req.returnDate) <= new Date(req.departureDate))
    return showAlert('alert1', 'Return date must be after departure date.');

  estimateReq = req;
  goPage(2);
  document.getElementById('loader2').classList.add('active');
  document.getElementById('budgetContent').style.display = 'none';

  try {
    const res = await fetch(`${API}/api/ai/estimate`, {
      method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token() },
      body: JSON.stringify(req)
    });

    if (res.status === 401) { localStorage.clear(); window.location.href = '/login.html'; return; }

    let data;
    const text = await res.text();
    try { data = text ? JSON.parse(text) : {}; }
    catch { throw new Error('Server returned an invalid response. Please try again.'); }

    if (!res.ok || !data.success) throw new Error(data?.message || `Server error (${res.status})`);
    estimation = data.data;
    renderBudgetCards();
    document.getElementById('loader2').classList.remove('active');
    document.getElementById('budgetContent').style.display = 'block';
  } catch (e) {
    document.getElementById('loader2').classList.remove('active');
    document.getElementById('budgetContent').style.display = 'block';
    showAlert('alert2', e.message);
  }
}

// ─── Render Budget Cards ──────────────────────────────────────────────────────
function renderBudgetCards() {
  const grid = document.getElementById('budgetGrid');
  const types = [
    { key: 'economy', label: 'Economy', icon: '🎒', data: estimation.economy },
    { key: 'premium', label: 'Premium', icon: '💼', data: estimation.premium },
    { key: 'luxury', label: 'Luxury', icon: '👑', data: estimation.luxury }
  ];
  grid.innerHTML = '';
  types.forEach(t => {
    const d = t.data;
    const avail = d.isAvailable;
    const card = document.createElement('div');
    card.className = `budget-card ${t.key}${avail ? '' : ' unavailable'}`;
    card.id = 'bc-' + t.key;
    card.innerHTML = `
      <div class="budget-icon">${t.icon}</div>
      <div class="budget-name">${t.label}</div>
      <div class="budget-range">$${fmt(d.minEstimate)} – $${fmt(d.maxEstimate)}</div>
      <div class="budget-sub">${avail ? 'Based on available data' : 'No data available for this type'}</div>
      ${avail ? `<div class="budget-breakdown">
        <div class="breakdown-item"><span>✈️ Flights</span><span>$${fmt(d.flightMinEstimate)}–$${fmt(d.flightMaxEstimate)}</span></div>
        <div class="breakdown-item"><span>🏨 Hotels</span><span>$${fmt(d.hotelMinEstimate)}–$${fmt(d.hotelMaxEstimate)}</span></div>
        <div class="breakdown-item"><span>🗺️ Tours</span><span>$${fmt(d.toursMinEstimate)}–$${fmt(d.toursMaxEstimate)}</span></div>
      </div>` : ''}`;
    if (avail) card.onclick = () => selectBudgetType(t.key, d.minEstimate, d.maxEstimate);
    grid.appendChild(card);
  });
}

function selectBudgetType(type, min, max) {
  selectedType = type;
  document.querySelectorAll('.budget-card').forEach(c => c.classList.remove('selected'));
  document.getElementById('bc-' + type).classList.add('selected');

  const slider = document.getElementById('budgetSlider');
  const mid = Math.round((min + max) / 2);
  slider.min = Math.round(min);
  slider.max = Math.round(max);
  slider.value = mid;
  document.getElementById('sliderMin').textContent = '$' + fmt(min);
  document.getElementById('sliderMax').textContent = '$' + fmt(max);
  updateSlider(mid);
  document.getElementById('sliderSection').style.display = 'block';
  document.getElementById('generateBtn').disabled = false;
  // Update slider gradient
  slider.style.background = `linear-gradient(to right,var(--primary) ${((mid - min) / (max - min)) * 100}%,var(--border) ${((mid - min) / (max - min)) * 100}%)`;
}

function updateSlider(val) {
  document.getElementById('budgetValDisplay').textContent = '$' + fmt(val);
  const slider = document.getElementById('budgetSlider');
  const pct = ((val - slider.min) / (slider.max - slider.min)) * 100;
  slider.style.background = `linear-gradient(to right,var(--primary) ${pct}%,var(--border) ${pct}%)`;
}

// ─── STEP 2 → 3: Generate Plan ───────────────────────────────────────────────
async function goGenerate() {
  if (!selectedType) return showAlert('alert2', 'Please select a budget type.');
  const maxBudget = parseFloat(document.getElementById('budgetSlider').value);
  const req = { ...estimateReq, budgetType: capitalize(selectedType), maxBudget };
  goPage(3);
  document.getElementById('loader3').classList.add('active');
  document.getElementById('planContent').style.display = 'none';

  try {
    const res = await fetch(`${API}/api/ai/generate-plan`, {
      method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token() },
      body: JSON.stringify(req)
    });
    const data = await res.json();
    if (!res.ok || !data.success) throw new Error(data.message || 'Plan generation failed');
    planResult = data.data;
    renderPlan();
    document.getElementById('loader3').classList.remove('active');
    document.getElementById('planContent').style.display = 'block';
  } catch (e) {
    document.getElementById('loader3').classList.remove('active');
    showAlert('alert3', e.message);
  }
}

// ─── Render Plan ─────────────────────────────────────────────────────────────
function renderPlan() {
  const p = planResult;

  // Summary header
  document.getElementById('planHeader').innerHTML = `
    <div class="plan-stat"><div class="plan-stat-val">$${fmt(p.estimatedTotalCost)}</div><div class="plan-stat-label">Total Cost</div></div>
    <div class="plan-stat"><div class="plan-stat-val">$${fmt(p.maxBudget)}</div><div class="plan-stat-label">Your Budget</div></div>
    <div class="plan-stat"><div class="plan-stat-val">${p.totalDays}</div><div class="plan-stat-label">Days</div></div>
    <div class="plan-stat"><div class="plan-stat-val">${p.adults + p.children}</div><div class="plan-stat-label">Travelers</div></div>
    <div class="plan-stat"><div class="plan-stat-val" style="text-transform:capitalize">${p.budgetType}</div><div class="plan-stat-label">Budget Type</div></div>`;

  // Flights
  const fb = document.getElementById('flightsBody');
  fb.innerHTML = '';
  if (p.goFlight) fb.appendChild(flightCard(p.goFlight));
  if (p.returnFlight) fb.appendChild(flightCard(p.returnFlight));
  if (!p.goFlight && !p.returnFlight)
    fb.innerHTML = `<div class="none-badge" style="background:rgba(138,92,246,0.1);border:1px dashed var(--border);color:var(--text);font-size:1rem;text-align:center;padding:14px;border-radius:12px">✈️ Allocated Flight Budget: <strong style="color:var(--accent);font-size:1.1rem">$${fmt(p.flightBudget)}</strong></div>`;

  // Flight budget summary
  const goPrice  = p.goFlight     ? (p.goFlight.totalPrice     || 0) : 0;
  const retPrice = p.returnFlight ? (p.returnFlight.totalPrice || 0) : 0;
  const totalFlightCost = goPrice + retPrice;
  const flightBudgetEl = document.getElementById('flightBudgetInfo');
  if (flightBudgetEl) {
    const showBudget = totalFlightCost > 0 ? totalFlightCost : p.flightBudget;
    if (showBudget > 0) {
      flightBudgetEl.innerHTML =
        `<span style="font-size:.85rem;color:var(--muted)">Flight budget:</span>
         <strong style="color:var(--accent);font-size:.95rem"> $${fmt(showBudget)}</strong>
         ${goPrice  > 0 ? `<span style="font-size:.75rem;color:var(--muted);margin-left:8px">GO $${fmt(goPrice)}</span>` : ''}
         ${retPrice > 0 ? `<span style="font-size:.75rem;color:var(--muted);margin-left:4px">· RETURN $${fmt(retPrice)}</span>` : ''}`;
    } else {
      flightBudgetEl.innerHTML = '';
    }
  }

  // City Plans
  const cb = document.getElementById('cityPlansBody');
  cb.innerHTML = '';
  p.cityPlans.forEach(city => cb.appendChild(cityCard(city)));
}


function flightCard(f) {
  const el = document.createElement('div');
  el.className = 'flight-card';
  el.innerHTML = `
    <span class="flight-dir">${f.direction}</span>
    <div class="flight-route">
      <div class="city-code">${f.departureAirportCode}</div>
      <div class="route-line"></div>
      <div class="city-code">${f.arrivalAirportCode}</div>
    </div>
    <div>
      <div style="font-size:.8rem;color:var(--muted)">${f.airlineName} · ${f.flightClass}</div>
      <div style="font-size:.8rem;color:var(--muted)">${fmtDate(f.departureTime)}</div>
      ${f.duration ? `<div style="font-size:.75rem;color:var(--muted)">${f.duration}</div>` : ''}
    </div>
    <div class="flight-price">$${fmt(f.totalPrice)}</div>`;
  return el;
}

function cityCard(city) {
  const el = document.createElement('div');
  el.className = 'city-plan';
  const hotelHtml = city.hotel ? `
    <div class="mini-card">
      <div class="mini-card-title">🏨 Hotel</div>
      <div class="mini-card-name">${city.hotel.hotelName}</div>
      <div class="stars">${'⭐'.repeat(city.hotel.starRating || 0)}</div>
      <div style="font-size:.8rem;color:var(--muted);margin:4px 0">${city.hotel.nights} nights · ${city.hotel.singleRooms} single, ${city.hotel.doubleRooms} double</div>
      <div class="mini-card-price">$${fmt(city.hotel.totalPrice)}</div>
    </div>` : `<div class="none-badge">🏨 No hotel found in budget</div>`;

  const tourHtml = city.tour ? `
    <div class="mini-card">
      <div class="mini-card-title">🗺️ Tour</div>
      <div class="mini-card-name">${city.tour.tourTitle}</div>
      <div style="font-size:.8rem;color:var(--muted)">Guide: ${city.tour.guideName}</div>
      <div style="font-size:.8rem;color:var(--muted)">${city.tour.durationHours || '?'}h · ${fmtDate(city.tour.availableDate)}</div>
      <div class="mini-card-price">$${fmt(city.tour.totalPrice)}</div>
    </div>` : `<div class="none-badge">🗺️ No tour found in budget</div>`;

  el.innerHTML = `
    <div class="city-plan-header">
      <div><div class="city-name">📍 ${city.city}</div><div class="city-dates">${fmtDate(city.checkIn)} → ${fmtDate(city.checkOut)}</div></div>
      <div style="text-align:right"><div style="font-size:.8rem;color:var(--muted)">Hotel budget: $${fmt(city.cityHotelBudget)}</div>
        <div style="font-size:.8rem;color:var(--muted)">Tours budget: $${fmt(city.cityToursBudget)}</div></div>
    </div>
    <div class="city-plan-body">${hotelHtml}${tourHtml}</div>`;
  return el;
}

// ─── Utils ────────────────────────────────────────────────────────────────────
const fmt = n => Number(n || 0).toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
const fmtDate = d => d ? new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) : '—';
const capitalize = s => s.charAt(0).toUpperCase() + s.slice(1);
