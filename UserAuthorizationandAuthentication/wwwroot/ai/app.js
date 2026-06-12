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
let hotelRegenerateIndex = 0;
let fixedItems = {
  goFlight: false,
  returnFlight: false,
  tours: [],
  hotel: false
};

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
    doubleRooms: isNaN(parseInt(document.getElementById('doubleRooms').value)) ? 1 : parseInt(document.getElementById('doubleRooms').value),
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
  hotelRegenerateIndex = 0;
  fixedItems = {
    goFlight: false,
    returnFlight: false,
    tours: [],
    hotel: false
  };
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
  // Flight budget split directly from backend
  const goB = p.goFlightBudget || 0;
  const retB = p.returnFlightBudget || 0;

  if (!p.goFlight && !p.returnFlight) {
    fb.innerHTML = `<div class="none-badge" style="background:rgba(138,92,246,0.1);border:1px dashed var(--border);color:var(--text);font-size:1rem;text-align:center;padding:14px;border-radius:12px">
      ✈️ Allocated Flight Budget: <strong style="color:var(--accent);font-size:1.1rem">$${fmt(p.flightBudget)}</strong>
      <div style="font-size:0.85rem; margin-top:8px; color:var(--text-muted)">
         GO: <strong>$${fmt(goB)}</strong> &nbsp;·&nbsp; RETURN: <strong>$${fmt(retB)}</strong>
      </div>
    </div>`;
  }

  // Flight budget summary
  const goPrice  = p.goFlight     ? (p.goFlight.totalPrice     || 0) : 0;
  const retPrice = p.returnFlight ? (p.returnFlight.totalPrice || 0) : 0;
  const totalFlightCost = goPrice + retPrice;
  const flightBudgetEl = document.getElementById('flightBudgetInfo');
  if (flightBudgetEl) {
    if (p.flightBudget > 0) {
      flightBudgetEl.innerHTML =
        `<span style="font-size:.85rem;color:var(--muted)">Allocated Budget: $${fmt(p.flightBudget)}</span>
         <span style="font-size:.75rem;color:var(--muted);margin-left:8px">GO $${fmt(goB)}</span>
         <span style="font-size:.75rem;color:var(--muted);margin-left:4px">· RETURN $${fmt(retB)}</span>
         ${totalFlightCost > 0 ? `<br><span style="font-size:.85rem;color:var(--muted)">Actual Cost:</span>
         <strong style="color:var(--accent);font-size:.95rem"> $${fmt(totalFlightCost)}</strong>
         ${goPrice  > 0 ? `<span style="font-size:.75rem;color:var(--muted);margin-left:8px">GO $${fmt(goPrice)}</span>` : ''}
         ${retPrice > 0 ? `<span style="font-size:.75rem;color:var(--muted);margin-left:4px">· RETURN $${fmt(retPrice)}</span>` : ''}` : ''}`;
    } else {
      flightBudgetEl.innerHTML = '';
    }
  }

  // City Plans
  const cb = document.getElementById('cityPlansBody');
  cb.innerHTML = '';
  p.cityPlans.forEach(city => cb.appendChild(cityCard(city)));

  // Debug Info Panel
  let debugEl = document.getElementById('debugInfoPanel');
  if (!debugEl) {
    debugEl = document.createElement('div');
    debugEl.id = 'debugInfoPanel';
    debugEl.style.cssText = 'margin-top:30px; padding:20px; background:rgba(0,0,0,0.2); border:1px dashed var(--glass-border); border-radius:12px; font-family:monospace; font-size:0.9rem; color:var(--text-muted); line-height:1.6; text-align:left;';
    
    // Insert before the action buttons at the bottom
    const actions = document.getElementById('planContent').querySelector('.btn-group');
    if (actions) {
      actions.parentElement.insertBefore(debugEl, actions);
    } else {
      document.getElementById('planContent').appendChild(debugEl);
    }
  }
  
  const d = p.debugData;
  if (d) {
    debugEl.innerHTML = `
      <div style="color:#c084fc; margin-bottom:10px; font-weight:bold; font-family:'Outfit', sans-serif;">🛠️ Debug Info</div>
      Median go: ${d.medianGo}<br>
      Median return: ${d.medianReturn}<br>
      num go: (${d.numGo})<br>
      num return: (${d.numReturn})<br>
      Median hotels single: ${d.medianHotelsSingle}<br>
      Median hotels double: ${d.medianHotelsDouble}<br>
      number of hotels: (${d.numHotelsSingle}, ${d.numHotelsDouble})<br>
      Median tours: ${d.medianTour}<br>
      num tours: (${d.numTours})<br><br>
      ${d.tourDebugPerCity ? `<div style="color:#c084fc; margin-top:5px; margin-bottom:5px; font-weight:bold;">🗺️ Tours Per City:</div>` + Object.entries(d.tourDebugPerCity).map(([city, info]) => ` &nbsp;&nbsp;· ${city}: ${info}`).join('<br>') + '<br><br>' : ''}
      ${d.hotelDebugPerCity ? `<div style="color:#c084fc; margin-top:5px; margin-bottom:5px; font-weight:bold;">🏨 Hotels Per City:</div>` + Object.entries(d.hotelDebugPerCity).map(([city, info]) => ` &nbsp;&nbsp;· ${city}: ${info}`).join('<br>') + '<br><br>' : ''}
      <div style="color:#10b981; margin-bottom:5px; font-weight:bold;">Hotel Recommender API Request:</div>
      <pre style="background:rgba(0,0,0,0.3); padding:10px; border-radius:8px; overflow-x:auto; color:#a7f3d0; margin:0; font-size: 0.8rem; margin-bottom: 10px;">${d.hotelApiRequestJson ? d.hotelApiRequestJson.replace(/</g, "&lt;") : 'N/A'}</pre>
      <div style="color:#3b82f6; margin-bottom:5px; font-weight:bold;">Hotel Recommender API Response:</div>
      <pre style="background:rgba(0,0,0,0.3); padding:10px; border-radius:8px; overflow-x:auto; color:#93c5fd; margin:0; font-size: 0.8rem; max-height: 300px; overflow-y: auto; margin-bottom: 10px;">${d.hotelApiResponseJson ? d.hotelApiResponseJson.replace(/</g, "&lt;") : 'API failed or returned nothing.'}</pre>
      <div style="color:#10b981; margin-bottom:5px; font-weight:bold;">Tour Recommender API Request:</div>
      <pre style="background:rgba(0,0,0,0.3); padding:10px; border-radius:8px; overflow-x:auto; color:#a7f3d0; margin:0; font-size: 0.8rem; margin-bottom: 10px;">${d.tourApiRequestJson ? d.tourApiRequestJson.replace(/</g, "&lt;") : 'N/A'}</pre>
      <div style="color:#3b82f6; margin-bottom:5px; font-weight:bold;">Tour Recommender API Response:</div>
      <pre style="background:rgba(0,0,0,0.3); padding:10px; border-radius:8px; overflow-x:auto; color:#93c5fd; margin:0; font-size: 0.8rem; max-height: 300px; overflow-y: auto;">${d.tourApiResponseJson ? d.tourApiResponseJson.replace(/</g, "&lt;") : 'API failed or returned nothing.'}</pre>
    `;
  } else {
    debugEl.style.display = 'none';
  }
}


function flightCard(f) {
  const el = document.createElement('div');
  el.className = 'flight-card';
  
  const formatTime = (d) => {
    if (!d) return '';
    const dt = new Date(d);
    return dt.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
  };
  
  const depTime = formatTime(f.departureTime);
  const arrTime = formatTime(f.arrivalTime);
  
  let stopsText = '';
  if (f.numberOfStops === 0) stopsText = 'Direct';
  else if (f.numberOfStops === 1) stopsText = '1 Stop';
  else if (f.numberOfStops > 1) stopsText = `${f.numberOfStops} Stops`;

  const isGo = f.direction.toLowerCase() === 'outbound';
  const isFixed = isGo ? fixedItems.goFlight : fixedItems.returnFlight;
  const fixText = isFixed ? '🔒 Fixed' : '🔓 Fix';
  const fixClass = isFixed ? 'btn-fixed' : 'btn-outline';

  el.innerHTML = `
    <span class="flight-dir">${f.direction}</span>
    <div class="flight-route">
      <div class="city-code">${f.departureAirportCode}</div>
      <div class="route-line"></div>
      <div class="city-code">${f.arrivalAirportCode}</div>
    </div>
    <div style="flex:1; padding: 0 15px;">
      ${depTime && arrTime ? `
      <div style="font-size:.9rem;font-weight:600;color:var(--text);margin-bottom:4px; letter-spacing:0.5px;">
        ${depTime} <span style="color:var(--muted);font-weight:400;margin:0 4px;">→</span> ${arrTime}
      </div>` : ''}
      <div style="font-size:.8rem;color:var(--muted); margin-bottom:2px;">${f.airlineName} · ${f.flightClass || 'Economy'}</div>
      <div style="font-size:.8rem;color:var(--muted)">${fmtDate(f.departureTime)}</div>
      <div style="font-size:.75rem;color:var(--muted); margin-top:6px; display:flex; gap:8px;">
        ${f.duration ? `<span style="background:rgba(255,255,255,0.05);padding:3px 8px;border-radius:6px;border:1px solid var(--border)">⏱️ ${f.duration}</span>` : ''}
        ${stopsText ? `<span style="background:rgba(255,255,255,0.05);padding:3px 8px;border-radius:6px;border:1px solid var(--border)">✈️ ${stopsText}</span>` : ''}
      </div>
    </div>
    <div style="text-align: right; display: flex; flex-direction: column; justify-content: center; align-items: flex-end;">
      <div class="flight-price">$${fmt(f.totalPrice)}</div>
      <button onclick="toggleFlightFix('${f.direction}')" class="btn ${fixClass}" style="margin-top:12px; font-size:0.75rem; padding:6px 12px">${fixText}</button>
    </div>
    `;
  return el;
}

async function regenerateFlight(sessionId, direction, btnElement) {
  const originalText = btnElement.innerHTML;
  btnElement.innerHTML = '🔄 Regenerating...';
  btnElement.disabled = true;

  try {
    const req = {
      sessionId: sessionId,
      adults: planResult.adults || 1,
      children: planResult.children || 0,
      direction: direction
    };

    const res = await fetch(`${API}/api/ai/regenerate-flight`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token() },
      body: JSON.stringify(req)
    });

    const newFlight = await res.json();
    if (!res.ok) throw new Error(newFlight.message || 'Failed to regenerate flight');

    // Replace the specific flight in planResult
    if (direction.toLowerCase() === 'outbound') {
      planResult.goFlight = newFlight;
    } else {
      planResult.returnFlight = newFlight;
    }
    
    // Re-render the UI
    renderPlan();

  } catch (e) {
    alert('Error regenerating flight: ' + e.message);
    btnElement.innerHTML = originalText;
    btnElement.disabled = false;
  }
}

async function regenerateTour(sessionId, dateToRegen, cityStr, btnElement) {
  const originalText = btnElement.innerHTML;
  btnElement.innerHTML = '🔄 Regenerating...';
  btnElement.disabled = true;

  try {
    const cityPlan = planResult.cityPlans.find(c => c.city === cityStr);
    const fixedDates = cityPlan.tours
        .filter(t => t.availableDate !== dateToRegen)
        .map(t => {
             const d = new Date(t.availableDate);
             return d.toISOString().split('T')[0];
        });

    const req = {
      sessionId: sessionId,
      fixedDates: fixedDates,
      totalPeople: (planResult.adults || 1) + (planResult.children || 0)
    };

    const res = await fetch(`${API}/api/ai/regenerate-tour`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token() },
      body: JSON.stringify(req)
    });

    const newTours = await res.json();
    if (!res.ok) throw new Error(newTours.message || 'Failed to regenerate tour');

    if (cityPlan) {
      cityPlan.tours = newTours;
    }
    
    renderPlan();

  } catch (e) {
    alert('Error regenerating tour: ' + e.message);
    btnElement.innerHTML = originalText;
    btnElement.disabled = false;
  }
}

function toggleHotelFix() {
  fixedItems.hotel = !fixedItems.hotel;
  renderPlan();
}

function cityCard(city) {
  const el = document.createElement('div');
  el.className = 'city-plan';
  
  const hotelHtml = city.hotel ? `
    <div class="flight-card" style="margin-top: 10px; border-left: 4px solid var(--primary); min-height: unset; padding: 15px 25px;">
      <span class="flight-dir" style="background: rgba(99, 102, 241, 0.15); color: var(--primary-light);">HOTEL</span>
      <div class="flight-route" style="flex: 0 0 120px; text-align: left;">
        <div class="city-code" style="font-size: 1.1rem; line-height: 1.3;">${city.hotel.nights} Nights</div>
        <div class="stars" style="margin-top: 4px;">${'⭐'.repeat(city.hotel.starRating || 0)}</div>
      </div>
      <div style="flex:1; padding: 0 15px; border-left: 1px solid var(--border); margin-left: 10px;">
        <div style="font-size:.95rem;font-weight:600;color:var(--text);margin-bottom:6px; letter-spacing:0.5px;">
          ${city.hotel.hotelName}
        </div>
        <div style="font-size:.8rem;color:var(--muted); margin-bottom:4px;">Rooms: ${city.hotel.singleRooms} Single, ${city.hotel.doubleRooms} Double</div>
        <div style="font-size:.75rem;color:var(--muted); margin-top:8px; display:flex; gap:8px;">
          <span style="background:rgba(255,255,255,0.05);padding:4px 10px;border-radius:6px;border:1px solid var(--border)">📍 ${city.city}</span>
        </div>
      </div>
      <div style="text-align: right; display: flex; flex-direction: column; justify-content: center; align-items: flex-end; min-width: 100px;">
        <div class="flight-price" style="color: var(--primary-light);">$${fmt(city.hotel.totalPrice)}</div>
        <button onclick="toggleHotelFix()" class="btn ${fixedItems.hotel ? 'btn-fixed' : 'btn-outline'}" style="margin-top:12px; font-size:0.75rem; padding:6px 12px">${fixedItems.hotel ? '🔒 Fixed' : '🔓 Fix'}</button>
      </div>
    </div>` : `<div class="none-badge" style="margin-top: 10px;">🏨 No hotel found in budget</div>`;

  let tourHtml = '';
  if (city.tours && city.tours.length > 0) {
    tourHtml = city.tours.map(t => {
      const tDateStr = new Date(t.availableDate).toISOString().split('T')[0];
      const isFixed = fixedItems.tours.includes(tDateStr);
      const fixText = isFixed ? '🔒 Fixed' : '🔓 Fix';
      const fixClass = isFixed ? 'btn-fixed' : 'btn-outline';
      return `
      <div class="flight-card" style="margin-top: 15px; border-left: 4px solid #10b981; min-height: unset; padding: 15px 25px;">
        <span class="flight-dir" style="background: rgba(16, 185, 129, 0.15); color: #10b981;">DAY TOUR</span>
        <div class="flight-route" style="flex: 0 0 120px; text-align: left;">
          <div class="city-code" style="font-size: 1.1rem; line-height: 1.3;">${fmtDate(t.availableDate)}</div>
        </div>
        <div style="flex:1; padding: 0 15px; border-left: 1px solid var(--border); margin-left: 10px;">
          <div style="font-size:.95rem;font-weight:600;color:var(--text);margin-bottom:6px; letter-spacing:0.5px;">
            ${t.tourTitle}
          </div>
          <div style="font-size:.8rem;color:var(--muted); margin-bottom:4px;">Guide: ${t.guideName || 'Auto-Assigned'}</div>
          <div style="font-size:.75rem;color:var(--muted); margin-top:8px; display:flex; gap:8px;">
            <span style="background:rgba(255,255,255,0.05);padding:4px 10px;border-radius:6px;border:1px solid var(--border)">⏱️ ${t.durationHours || '?'} hr</span>
            <span style="background:rgba(255,255,255,0.05);padding:4px 10px;border-radius:6px;border:1px solid var(--border)">⭐ ${t.rating || 'New'} (${t.numberOfReviews || 0})</span>
          </div>
        </div>
        <div style="text-align: right; display: flex; flex-direction: column; justify-content: center; align-items: flex-end; min-width: 100px;">
          <div class="flight-price" style="color: #10b981;">$${fmt(t.totalPrice)}</div>
          <button onclick="toggleTourFix('${tDateStr}')" class="btn ${fixClass}" style="margin-top:12px; font-size:0.75rem; padding:6px 12px; border-color: rgba(16,185,129,0.5); color: #10b981;">${fixText}</button>
        </div>
      </div>`;
    }).join('');
  } else if (city.tour) {
    const t = city.tour;
    const tDateStr = new Date(t.availableDate).toISOString().split('T')[0];
    const isFixed = fixedItems.tours.includes(tDateStr);
    const fixText = isFixed ? '🔒 Fixed' : '🔓 Fix';
    const fixClass = isFixed ? 'btn-fixed' : 'btn-outline';
    tourHtml = `
      <div class="flight-card" style="margin-top: 15px; border-left: 4px solid #10b981; min-height: unset; padding: 15px 25px;">
        <span class="flight-dir" style="background: rgba(16, 185, 129, 0.15); color: #10b981;">TOUR</span>
        <div class="flight-route" style="flex: 0 0 120px; text-align: left;">
          <div class="city-code" style="font-size: 1.1rem; line-height: 1.3;">${fmtDate(t.availableDate)}</div>
        </div>
        <div style="flex:1; padding: 0 15px; border-left: 1px solid var(--border); margin-left: 10px;">
          <div style="font-size:.95rem;font-weight:600;color:var(--text);margin-bottom:6px; letter-spacing:0.5px;">
            ${t.tourTitle}
          </div>
          <div style="font-size:.8rem;color:var(--muted); margin-bottom:4px;">Guide: ${t.guideName || 'Auto-Assigned'}</div>
          <div style="font-size:.75rem;color:var(--muted); margin-top:8px; display:flex; gap:8px;">
            <span style="background:rgba(255,255,255,0.05);padding:4px 10px;border-radius:6px;border:1px solid var(--border)">⏱️ ${t.durationHours || '?'} hr</span>
          </div>
        </div>
        <div style="text-align: right; display: flex; flex-direction: column; justify-content: center; align-items: flex-end; min-width: 100px;">
          <div class="flight-price" style="color: #10b981;">$${fmt(t.totalPrice)}</div>
          <button onclick="toggleTourFix('${tDateStr}')" class="btn ${fixClass}" style="margin-top:12px; font-size:0.75rem; padding:6px 12px; border-color: rgba(16,185,129,0.5); color: #10b981;">${fixText}</button>
        </div>
      </div>`;
  } else {
    tourHtml = `<div class="none-badge" style="margin-top: 10px;">🗺️ No tours found in budget</div>`;
  }

  el.innerHTML = `
    <div class="city-plan-header">
      <div><div class="city-name">📍 ${city.city}</div><div class="city-dates">${fmtDate(city.checkIn)} → ${fmtDate(city.checkOut)}</div></div>
      <div style="text-align:right">
        <div style="font-size:.8rem;color:var(--muted)">Hotel budget: $${fmt(city.cityHotelBudget)}</div>
        <div style="font-size:.8rem;color:var(--muted)">Tours budget: $${fmt(city.cityToursBudget)}</div>
      </div>
    </div>
    <div style="padding: 24px; display: flex; flex-direction: column; gap: 24px;">
      <div>
        <div style="font-size: 0.8rem; font-weight: 700; color: var(--muted); margin-bottom: 10px; text-transform: uppercase; letter-spacing: 1px;">🏨 Accommodation</div>
        ${hotelHtml}
      </div>
      <div>
        <div style="font-size: 0.8rem; font-weight: 700; color: var(--muted); margin-bottom: 10px; text-transform: uppercase; letter-spacing: 1px;">🗺️ Activities & Tours</div>
        ${tourHtml}
      </div>
    </div>`;
  return el;
}

function toggleFlightFix(direction) {
  const isGo = direction.toLowerCase() === 'outbound';
  if (isGo) {
    fixedItems.goFlight = !fixedItems.goFlight;
  } else {
    fixedItems.returnFlight = !fixedItems.returnFlight;
  }
  renderPlan();
}

function toggleTourFix(dateStr) {
  const idx = fixedItems.tours.indexOf(dateStr);
  if (idx > -1) {
    fixedItems.tours.splice(idx, 1);
  } else {
    fixedItems.tours.push(dateStr);
  }
  renderPlan();
}

async function goRegenerateUnified() {
  const btn = document.getElementById('unifiedRegenBtn');
  const originalText = btn.innerHTML;
  btn.innerHTML = '🔄 Regenerating...';
  btn.disabled = true;

  document.getElementById('loader3').classList.add('active');
  document.getElementById('planContent').style.display = 'none';

  if (!fixedItems.hotel) {
    hotelRegenerateIndex++;
  }

  try {
    const req = {
      goFlightSessionId: planResult.goFlight?.sessionId || null,
      returnFlightSessionId: planResult.returnFlight?.sessionId || null,
      tourSessionId: planResult.tourSessionId || null,

      isGoFlightFixed: fixedItems.goFlight,
      fixedGoFlightId: fixedItems.goFlight ? (planResult.goFlight?.id || null) : null,

      isReturnFlightFixed: fixedItems.returnFlight,
      fixedReturnFlightId: fixedItems.returnFlight ? (planResult.returnFlight?.id || null) : null,

      hotelRegenerateIndex: hotelRegenerateIndex,
      isHotelFixed: fixedItems.hotel,
      fixedHotelId: fixedItems.hotel ? (planResult.cityPlans.find(cp => cp.hotel)?.hotel?.id || null) : null,

      fixedTourDates: fixedItems.tours,

      fromCity: estimateReq.fromCity,
      fromCountry: estimateReq.fromCountry || '',
      toCity: estimateReq.toCity,
      toCountry: estimateReq.toCountry || '',
      departureDate: estimateReq.departureDate,
      returnDate: estimateReq.returnDate,
      adults: estimateReq.adults,
      children: estimateReq.children,
      singleRooms: estimateReq.singleRooms,
      doubleRooms: estimateReq.doubleRooms,
      touristLanguage: estimateReq.touristLanguage,
      excludeFlights: estimateReq.excludeFlights,
      excludeHotels: estimateReq.excludeHotels,
      excludeTours: estimateReq.excludeTours,
      itinerary: estimateReq.itinerary,
      budgetType: capitalize(selectedType),
      maxBudget: parseFloat(document.getElementById('budgetSlider').value)
    };

    const res = await fetch(`${API}/api/ai/regenerate-plan`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + token() },
      body: JSON.stringify(req)
    });
    
    const data = await res.json();
    if (!res.ok || !data.success) throw new Error(data.message || 'Regeneration failed');

    planResult = data.data;
    renderPlan();
  } catch (e) {
    showAlert('alert3', e.message);
  } finally {
    document.getElementById('loader3').classList.remove('active');
    document.getElementById('planContent').style.display = 'block';
    btn.innerHTML = originalText;
    btn.disabled = false;
  }
}

// ─── Utils ────────────────────────────────────────────────────────────────────
const fmt = n => Number(n || 0).toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
const fmtDate = d => d ? new Date(d).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) : '—';
const capitalize = s => s.charAt(0).toUpperCase() + s.slice(1);
