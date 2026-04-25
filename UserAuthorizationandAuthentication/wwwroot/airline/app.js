const API_URL = '/api';
let API_TOKEN = localStorage.getItem('token');
let USER_ROLE = localStorage.getItem('role');

// Initialize
document.addEventListener('DOMContentLoaded', async () => {
    console.log('App Started. API URL:', API_URL);
    updateUIForAuth();
    loadAirlinesFilter();
    if (API_TOKEN) {
        try {
            const res = await apiFetch('/Users/profile');
            localStorage.setItem('userId', res.data.id);
            fetchWalletBalance();
            switchTab('dashboard'); // Load dashboard by default if logged in
        } catch (e) { 
            console.error('Token invalid or expired'); 
            switchTab('flights');
        }
    } else {
        switchTab('flights');
    }
});

async function fetchWalletBalance() {
    if (!API_TOKEN) return;
    try {
        const res = await apiFetch('/airline/wallet/balance');
        document.getElementById('wallet-balance-nav').innerText = res.data.balance.toFixed(2);
        const dashWallet = document.getElementById('dash-wallet-balance-card');
        if (dashWallet) {
            dashWallet.innerText = res.data.balance.toFixed(2);
        }
        document.getElementById('wallet-container').style.display = 'flex';
    } catch (e) { console.error('Failed to parse wallet', e); }
}

async function showAddFundsModal() {
    const modalBody = document.getElementById('modal-body');
    modalBody.innerHTML = `
        <div style="text-align:center; padding-top:10px">
            <h3 style="margin-bottom:15px">Top Up Wallet</h3>
            <p style="color:var(--text-gray); font-size:12px; margin-bottom:20px">Add funds securely using your credit card.</p>
            <input type="number" id="fund-amount" placeholder="Amount (USD)" style="font-size:18px; text-align:center; width:200px; margin-bottom:15px">
            <button class="btn" style="background:var(--success)" onclick="processAddFunds()">💳 Add Funds</button>
        </div>
    `;
    showModal('Wallet Setup');
}

async function processAddFunds() {
    const amount = parseFloat(document.getElementById('fund-amount').value);
    if (!amount || amount <= 0) { alert('Enter a valid amount'); return; }
    try {
        await apiFetch('/airline/wallet/add-funds', {
            method: 'POST',
            body: JSON.stringify({ amount, paymentMethod: 'CreditCard' })
        });
        alert('Funds added successfully!');
        fetchWalletBalance();
        closeModal();
    } catch(e) { alert(e.message); }
}

// Generic Fetch Wrapper
async function apiFetch(endpoint, options = {}) {
    const headers = { 'Content-Type': 'application/json', ...options.headers };
    if (API_TOKEN) headers['Authorization'] = `Bearer ${API_TOKEN}`;

    try {
        const res = await fetch(`${API_URL}${endpoint}`, { ...options, headers });

        // Handle empty or non-JSON responses
        const contentType = res.headers.get("content-type");
        let data = { success: true, data: null };

        if (contentType && contentType.includes("application/json")) {
            const text = await res.text();
            if (text) {
                data = JSON.parse(text);
            }
        } else {
            console.warn('Non-JSON response received');
        }

        if (!res.ok) throw new Error(data.message || `Server Error (${res.status})`);
        return data.data !== undefined ? data : { success: true, data: data };
    } catch (err) {
        console.error(`Error fetching ${endpoint}:`, err);
        throw err;
    }
}

// --- Auth ---
async function login(email, password) {
    try {
        const data = await apiFetch('/Users/login', {
            method: 'POST',
            body: JSON.stringify({ email, password })
        });
        const authData = data.data.data ? data.data.data : data.data;
        API_TOKEN = authData.token || authData.Token;
        USER_ROLE = authData.role || authData.Role;
        localStorage.setItem('token', API_TOKEN);
        localStorage.setItem('role', USER_ROLE);
        localStorage.setItem('userId', authData.userId || authData.UserId);
        updateUIForAuth();
        closeModal();
        alert('Welcome back!');
        location.reload();
    } catch (e) { alert('Login failed: ' + e.message); }
}

async function register(userData) {
    try {
        await apiFetch('/Users/register', {
            method: 'POST',
            body: JSON.stringify(userData)
        });
        alert('Registration successful! Please login.');
        showAuth(); // Go back to login
    } catch (e) { alert('Registration failed: ' + e.message); }
}

function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('userValues');
    localStorage.removeItem('role');
    localStorage.removeItem('userId');
    window.location.href = '/auth/login.html';
}

function updateUIForAuth() {
    const userInfo = document.getElementById('user-info');
    if (API_TOKEN) {
        userInfo.innerHTML = `
            <div id="wallet-container" style="display: flex; align-items: center; gap: 8px; background: rgba(34, 197, 94, 0.1); padding: 8px 15px; border-radius: 20px; border: 1px solid rgba(34, 197, 94, 0.2); font-weight: 600; color: #4ade80; cursor: pointer;" onclick="showAddFundsModal()">
                💰 $<span id="wallet-balance-nav">...</span>
            </div>
            <span style="color:var(--text-gray); margin-right:15px; margin-left:15px">Role: ${USER_ROLE}</span>
            <button class="btn btn-secondary" style="width:auto; padding:10px 14px" onclick="switchTab('profile')" title="My Profile">
                👤
            </button>
            <button class="btn btn-secondary" style="width:auto; padding:10px 14px" onclick="switchTab('settings')" title="Settings">
                ⚙️
            </button>
            <button class="btn" style="width:auto; background:var(--danger)" onclick="logout()">Logout</button>
        `;
        if (USER_ROLE === 'Admin' || USER_ROLE === 'AirlineCompany') {
            document.querySelectorAll('.admin-only').forEach(el => el.style.display = 'block');
        }
        document.querySelectorAll('.user-logged-in').forEach(el => el.style.display = 'inline-block');
        fetchWalletBalance();
    }
}

// --- Flights ---
async function loadAirlinesFilter() {
    try {
        const res = await apiFetch('/airline/airlines');
        const select = document.getElementById('filter-airlines');
        if (!select) return;
        
        const airlines = res.data || [];
        airlines.forEach(a => {
            select.innerHTML += `<option value="${a.id}" style="color: black;">${a.name}</option>`;
        });
    } catch (e) {
        console.error('Failed to load airlines for filter', e);
    }
}

async function loadFlights() {
    try {
        const from = document.getElementById('filter-from')?.value;
        const to = document.getElementById('filter-to')?.value;
        const date = document.getElementById('filter-date')?.value;
        const stops = document.getElementById('filter-stops')?.value;
        const airlineId = document.getElementById('filter-airlines')?.value;
        const minPrice = document.getElementById('filter-min-price')?.value;
        const maxPrice = document.getElementById('filter-max-price')?.value;

        const body = {};
        if (from) body.from = from;
        if (to) body.to = to;
        if (date) body.date = date;
        if (stops !== undefined && stops !== '') body.maxStops = parseInt(stops);
        if (airlineId !== undefined && airlineId !== '') body.airlineIds = [parseInt(airlineId)];
        if (minPrice) body.minPrice = parseFloat(minPrice);
        if (maxPrice) body.maxPrice = parseFloat(maxPrice);

        console.log('Searching flights with filters:', body);
        const response = await apiFetch('/airline/flights/search', {
            method: 'POST',
            body: JSON.stringify(body)
        });
        const list = document.getElementById('flights-list');
        list.innerHTML = '';

        const flightsData = response.data.flights || response.data;

        if (!flightsData || flightsData.length === 0) {
            list.innerHTML = '<div style="grid-column: 1/-1; text-align: center; padding: 50px;">No flights found matching your criteria.</div>';
            return;
        }

        flightsData.forEach(f => {
            list.innerHTML += `
                <div class="card" style="padding:0; overflow:hidden">
                    <div style="height:150px; background:url('${(f.destinationImageUrl ? f.destinationImageUrl.trim() : null) || 'https://images.unsplash.com/photo-1436491865332-7a61a109c055?q=80&w=2070'}') center/cover; position:relative">
                        <div style="position:absolute; bottom:0; left:0; right:0; padding:15px; background:linear-gradient(transparent, rgba(0,0,0,0.8)); color:white">
                            <span style="font-size:12px; opacity:0.8">${f.airlineName}</span>
                            <h3 style="margin:0">${f.fromCode} ➔ ${f.toCode}</h3>
                        </div>
                    </div>
                    <div style="padding:20px">
                        <p style="color:var(--text-gray); margin: 0 0 15px 0; font-size:14px">${f.departureTime ? new Date(f.departureTime).toLocaleString([], { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }) : 'TBD'}</p>
                        <div style="display:flex; justify-content:space-between; align-items:center">
                            <span style="font-size:24px; font-weight:700">$${f.price}</span>
                            <div style="display:flex; gap:8px">
                                <button class="btn btn-secondary" style="width:auto; padding:8px 15px" onclick="viewFlightDetails(${f.id})">Info</button>
                                <button class="btn" style="width:auto; padding:8px 15px" onclick="openBookingModal(${f.id}, '${f.fromCode} to ${f.toCode}')">Book</button>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        });
    } catch (e) {
        console.error('Failed to load flights:', e);
        document.getElementById('flights-list').innerHTML = `<div style="grid-column: 1/-1; text-align: center; padding: 50px; color:var(--danger)">Connection Error: ${e.message}</div>`;
    }
}

function swapCities() {
    const from = document.getElementById('filter-from');
    const to = document.getElementById('filter-to');
    const temp = from.value;
    from.value = to.value;
    to.value = temp;
}

function resetFilters() {
    ['filter-from', 'filter-to', 'filter-date', 'filter-stops', 'filter-airlines', 'filter-min-price', 'filter-max-price'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = '';
    });
    
    // reset sorting if exists
    const sortEl = document.getElementById('filter-sort');
    if (sortEl) sortEl.value = 'departure';

    loadFlights();
}

async function viewFlightDetails(id) {
    try {
        const res = await apiFetch(`/airline/flights/${id}`);
        const f = res.data;

        // Load reviews for this flight
        let reviewsHTML = '';
        try {
            const reviewsRes = await apiFetch(`/airline/reviews/flight/${id}`);
            const reviews = reviewsRes.data || [];

            if (reviews.length > 0) {
                reviewsHTML = `
                    <div style="margin-top:20px; padding-top:20px; border-top:1px solid var(--glass-border)">
                        <h4 style="margin-bottom:15px">Passenger Reviews (${reviews.length})</h4>
                        <div style="max-height:200px; overflow-y:auto; display:flex; flex-direction:column; gap:10px">
                            ${reviews.map(r => `
                                <div style="background:rgba(255,255,255,0.05); padding:12px; border-radius:10px">
                                    <div style="display:flex; justify-content:space-between; margin-bottom:5px">
                                        <strong>${r.userName}</strong>
                                        <span style="color:var(--primary)">${'⭐'.repeat(r.rating)}</span>
                                    </div>
                                    <p style="font-size:13px; color:var(--text-gray); margin:0">${r.comment || 'No comment'}</p>
                                    <p style="font-size:10px; opacity:0.6; margin-top:5px">${new Date(r.createdAt).toLocaleDateString()}</p>
                                </div>
                            `).join('')}
                        </div>
                    </div>
                `;
            }
        } catch (e) { console.error('Failed to load reviews'); }

        const modalBody = document.getElementById('modal-body');

        modalBody.innerHTML = `
            <div style="padding:0; overflow:hidden; border-radius:12px">
                <div style="height:200px; background:url('${(f.destinationImageUrl ? f.destinationImageUrl.trim() : null) || 'https://images.unsplash.com/photo-1436491865332-7a61a109c055?q=80&w=2070'}') center/cover; position:relative; margin-bottom:20px">
                    <div style="position:absolute; bottom:0; left:0; right:0; padding:20px; background:linear-gradient(transparent, rgba(0,0,0,0.9)); color:white">
                        <h2 style="margin:0">${f.airlineName}</h2>
                        <p style="margin:5px 0 0; opacity:0.8; font-size:14px">Flight #${f.flightNumber || f.id}</p>
                    </div>
                </div>

                <div style="padding:0 20px 20px">
                    <div style="display:grid; grid-template-columns: 1fr auto 1fr; gap:20px; align-items:center; margin-bottom:25px; text-align:center">
                    <div>
                        <p style="font-size:24px; font-weight:800; margin:0">${f.fromCode}</p>
                        <p style="font-size:12px; color:var(--text-gray); margin:5px 0">${f.fromCity}, ${f.fromCountry}</p>
                        <p style="font-size:14px; font-weight:600">${new Date(f.departureTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</p>
                    </div>
                    <div style="color:var(--primary); font-size:24px">➔</div>
                    <div>
                        <p style="font-size:24px; font-weight:800; margin:0">${f.toCode}</p>
                        <p style="font-size:12px; color:var(--text-gray); margin:5px 0">${f.toCity}, ${f.toCountry}</p>
                        <p style="font-size:14px; font-weight:600">${new Date(f.arrivalTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</p>
                    </div>
                </div>

                <div style="display:grid; grid-template-columns: 1fr 1fr; gap:15px">
                    <div style="background:rgba(255,255,255,0.05); padding:15px; border-radius:12px; border:1px solid var(--glass-border)">
                        <p style="color:var(--text-gray); font-size:12px; margin-bottom:5px">Available Seats</p>
                        <p style="font-size:18px; font-weight:700; color:var(--success)">${f.availableSeats} Left</p>
                    </div>
                    <div style="background:rgba(255,255,255,0.05); padding:15px; border-radius:12px; border:1px solid var(--glass-border)">
                        <p style="color:var(--text-gray); font-size:12px; margin-bottom:5px">Stops</p>
                        <p style="font-size:18px; font-weight:700">${f.numberOfStops === 0 ? 'Direct' : f.numberOfStops + ' Stop(s)'}</p>
                    </div>
                    <div style="background:rgba(255,255,255,0.05); padding:15px; border-radius:12px; border:1px solid var(--glass-border)">
                        <p style="color:var(--text-gray); font-size:12px; margin-bottom:5px">Price Per Seat</p>
                        <p style="font-size:18px; font-weight:700">$${f.price}</p>
                    </div>
                    <div style="background:rgba(255,255,255,0.05); padding:15px; border-radius:12px; border:1px solid var(--glass-border)">
                        <p style="color:var(--text-gray); font-size:12px; margin-bottom:5px">Status</p>
                        <p style="font-size:18px; font-weight:700; color:var(--primary)">${f.status}</p>
                    </div>
                </div>

                ${(USER_ROLE === 'Admin' || USER_ROLE === 'AirlineCompany') ? `
                    <div style="margin-top:15px; padding:12px; background:rgba(59,130,246,0.1); border-radius:12px; border:1px solid rgba(59,130,246,0.2)">
                        <p style="font-size:12px; color:var(--text-gray); margin:0">Security Tracking:</p>
                        <p style="font-size:14px; margin:5px 0 0">Added By: <strong>${f.createdByUserName || 'System'}</strong></p>
                    </div>
                ` : ''}

                ${reviewsHTML}

                <div style="display:flex; gap:10px; margin-top:25px">
                    <button class="btn" style="flex:1" onclick="openBookingModal(${f.id}, '${f.fromCode} to ${f.toCode}')">Book This Flight</button>
                    ${API_TOKEN ? `<button class="btn" style="flex:1; background:rgba(255,255,255,0.1)" onclick="showAddReviewForm(${f.id})">Add Review</button>` : ''}
                </div>
            </div>
        `;
        showModal('Flight Information');
    } catch (e) { alert('Failed to load flight details: ' + e.message); }
}

// Duplicated resetFilters function removed.

// --- Companions ---
async function loadCompanions() {
    try {
        const data = await apiFetch('/airline/companions');
        const list = document.getElementById('companions-list');
        list.innerHTML = '';
        const companions = data.data || [];

        if (companions.length === 0) {
            list.innerHTML = `
                <div style="grid-column: 1/-1; text-align:center; padding:50px">
                    <div style="font-size:48px; margin-bottom:15px; opacity:0.3">👥</div>
                    <h3 style="margin-bottom:10px">No Companions Yet</h3>
                    <p style="color:var(--text-gray); margin-bottom:20px">Add travel companions to make booking easier</p>
                    <button class="btn" style="width:auto; padding:10px 30px" onclick="showAddCompanionForm()">Add Your First Companion</button>
                </div>
            `;
            return;
        }

        companions.forEach(c => {
            list.innerHTML += `
                <div class="card" style="display:flex; gap:15px; align-items:center">
                    <img src="${c.profilePic || 'https://ui-avatars.com/api/?name=' + c.firstName + '+' + c.lastName}" style="width:50px; height:50px; border-radius:50%; object-fit:cover; background:var(--glass-border)">
                    <div style="flex:1">
                        <h4 style="margin:0">${c.firstName} ${c.lastName}</h4>
                        <p style="color:var(--text-gray); font-size:12px; margin:2px 0">${c.ageType} | ${c.nationality}</p>
                        <p style="font-size:11px; color:var(--primary)">Pass: ${c.passportNumber || 'N/A'}</p>
                    </div>
                    <button onclick="deleteCompanion(${c.id})" style="background:none; border:none; color:var(--danger); cursor:pointer; font-size:24px; transition:0.3s" onmouseover="this.style.transform='scale(1.2)'" onmouseout="this.style.transform='scale(1)'">×</button>
                </div>
            `;
        });
    } catch (e) {
        console.error('Failed to load companions');
        document.getElementById('companions-list').innerHTML = `
            <div style="grid-column: 1/-1; text-align:center; padding:50px; color:var(--danger)">
                <p>Failed to load companions. Please try again.</p>
            </div>
        `;
    }
}

async function addCompanion() {
    const companionData = {
        firstName: document.getElementById('c-fname').value,
        lastName: document.getElementById('c-lname').value,
        ageType: document.getElementById('c-age').value,
        passportNumber: document.getElementById('c-pass').value,
        nationality: document.getElementById('c-nat').value,
        profilePic: document.getElementById('c-profile').value,
        passportImage: document.getElementById('c-p-image').value
    };
    try {
        await apiFetch('/airline/companions', { method: 'POST', body: JSON.stringify(companionData) });
        loadCompanions();
        closeModal();
    } catch (e) { alert(e.message); }
}

async function deleteCompanion(id) {
    if (!confirm('Are you sure you want to delete this companion?')) return;
    try {
        await apiFetch(`/airline/companions/${id}`, { method: 'DELETE' });
        loadCompanions();
    } catch (e) { alert('Failed to delete companion: ' + e.message); }
}

// --- Bookings ---
async function openBookingModal(flightId, route) {
    if (!API_TOKEN) { showAuth(); return; }
    try {
        const data = await apiFetch('/airline/companions');
        const companions = data.data || [];
        const modalBody = document.getElementById('modal-body');

        modalBody.innerHTML = `
            <p style="margin-bottom:15px; color:var(--text-gray)">Travelers for <strong>${route}</strong>:</p>
            
            <label style="display:block; margin-bottom:12px; padding:10px; background:rgba(255,255,255,0.05); border-radius:10px; cursor:pointer; border:1px solid var(--primary)">
                <input type="checkbox" id="self-booking" checked style="width:auto; margin-right:10px">
                Book for Me (Account Holder)
            </label>

            <div id="companion-selection" style="max-height:200px; overflow-y:auto; margin-top:10px">
                ${companions.length > 0 ? companions.map(c => `
                    <label style="display:block; margin-bottom:10px; cursor:pointer; padding:5px">
                        <input type="checkbox" name="comp" value="${c.id}" style="width:auto; margin-right:10px">
                        ${c.firstName} ${c.lastName} (${c.ageType})
                    </label>
                `).join('') : '<p style="font-size:12px; color:var(--text-gray); padding:10px">No companions added yet.</p>'}
            </div>
            
            <p style="font-size:12px; margin-top:15px; color:var(--text-gray)">Seats will be calculated automatically.</p>
            <button class="btn" style="margin-top:10px" onclick="confirmBooking(${flightId})">Confirm & Pay</button>
        `;
        showModal(`Book Flight`);
    } catch (e) { alert('Error loading companions'); }
}

async function confirmBooking(flightId) {
    const includeSelf = document.getElementById('self-booking').checked;
    const selectedCompanions = Array.from(document.querySelectorAll('input[name="comp"]:checked')).map(i => parseInt(i.value));

    if (!includeSelf && selectedCompanions.length === 0) {
        alert('Please select at least yourself or one companion.');
        return;
    }

    const totalSeats = (includeSelf ? 1 : 0) + selectedCompanions.length;

    try {
        await apiFetch('/airline/bookings', {
            method: 'POST',
            body: JSON.stringify({
                flightId: flightId,
                numberOfSeats: totalSeats,
                companionIds: selectedCompanions
            })
        });
        alert('Booking Confirmed! Payment deducted from your wallet.');
        fetchWalletBalance();
        switchTab('bookings');
        closeModal();
    } catch (e) { alert(e.message); }
}

async function loadMyBookings() {
    try {
        const data = await apiFetch('/airline/bookings/my-bookings');
        const list = document.getElementById('bookings-list');
        list.innerHTML = '';
        const bookings = data.data || [];

        if (bookings.length === 0) {
            list.innerHTML = `
                <div style="grid-column: 1/-1; text-align:center; padding:50px">
                    <div style="font-size:48px; margin-bottom:15px; opacity:0.3">✈️</div>
                    <h3 style="margin-bottom:10px">No Bookings Yet</h3>
                    <p style="color:var(--text-gray); margin-bottom:20px">Start your journey by booking your first flight</p>
                    <button class="btn" style="width:auto; padding:10px 30px" onclick="switchTab('flights')">Browse Flights</button>
                </div>
            `;
            return;
        }

        bookings.forEach(b => {
            list.innerHTML += `
                <div class="card">
                    <div style="display:flex; justify-content:space-between">
                        <span class="status-badge status-${(b.status || 'pending').toLowerCase()}">${b.status || 'Pending'}</span>
                        <span style="font-size:12px; color:var(--text-gray)">${new Date(b.bookingDate).toLocaleDateString()}</span>
                    </div>
                    <h3 style="margin-top:10px">${b.fromCode} -> ${b.toCode}</h3>
                    <p style="color:var(--primary); font-weight:700">$${b.totalPrice}</p>
                    <div style="margin-top:10px; border-top:1px solid var(--glass-border); padding-top:10px">
                        ${(b.passengers || []).map(p => `
                            <div style="margin-bottom:5px">
                                <div style="display:flex; justify-content:space-between; font-size:14px">
                                    <span>${p.firstName}</span>
                                    <span class="status-badge status-${(p.status || 'pending').toLowerCase()}" style="font-size:10px">${p.status || 'Pending'}</span>
                                </div>
                                ${p.status === 'Rejected' && p.rejectionReason ? `<div style="font-size:11px; color:#f87171; margin-left:5px">↳ ${p.rejectionReason}</div>` : ''}
                            </div>
                        `).join('')}
                    </div>
                    ${b.status === 'Rejected' && b.rejectionReason ? `
                        <div style="margin-top:10px; padding:8px; background:rgba(239,68,68,0.1); border-radius:8px; color:#f87171; font-size:12px">
                            <strong>Reason:</strong> ${b.rejectionReason}
                        </div>
                    ` : ''}
                    ${b.status === 'Confirmed' ? `
                        <button class="btn" style="margin-top:10px; padding:8px; background:var(--success)" onclick="showETicket(${b.id})">🎟️ View E-Ticket</button>
                    ` : ''}
                    <button class="btn" style="margin-top:10px; padding:5px; font-size:12px; background:var(--glass-border)" onclick="viewBookingDetails(${b.id})">Details</button>
                </div>
            `;
        });
    } catch (e) {
        console.error('Failed to load bookings');
        document.getElementById('bookings-list').innerHTML = `
            <div style="grid-column: 1/-1; text-align:center; padding:50px; color:var(--danger)">
                <p>Failed to load bookings. Please try again.</p>
            </div>
        `;
    }
}

async function showETicket(bookingId) {
    try {
        const res = await apiFetch('/airline/bookings/' + bookingId + '/eticket');
        const eticket = res.data;
        const modalBody = document.getElementById('modal-body');

        modalBody.innerHTML = `
            <div style="max-height: 500px; overflow-y:auto; padding-right:10px">
                <div style="text-align:center; margin-bottom:20px">
                    <h1 style="margin:0; font-size:32px; font-weight:800; letter-spacing:2px">BOARDING PASS</h1>
                    <p style="color:var(--text-gray); margin-top:5px; font-size:14px">Booking Ref: <strong>${eticket.pnr}</strong></p>
                </div>
                
                <div style="background: white; color: black; border-radius: 16px; padding: 20px; box-shadow: 0 10px 30px rgba(0,0,0,0.5)">
                    <div style="display:flex; justify-content:space-between; align-items:center; border-bottom:2px dashed #ccc; padding-bottom:15px; margin-bottom:15px">
                        <div>
                            <p style="font-size:12px; color:#666; margin:0">Carrier</p>
                            <h3 style="margin:0; color:#1e293b">${eticket.airlineName}</h3>
                        </div>
                        <div style="text-align:right">
                            <p style="font-size:12px; color:#666; margin:0">Class</p>
                            <h3 style="margin:0; color:#1e293b">Economy</h3>
                        </div>
                    </div>

                    <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:20px">
                        <div>
                            <h2 style="margin:0; font-size:40px; color:#1e293b">${eticket.departureAirport}</h2>
                            <p style="margin:0; color:#666">${new Date(eticket.departureTime).toLocaleString([], {month:'short', day:'numeric', hour:'2-digit', minute:'2-digit'})}</p>
                        </div>
                        <div style="font-size:24px; color:#94a3b8">✈️</div>
                        <div style="text-align:right">
                            <h2 style="margin:0; font-size:40px; color:#1e293b">${eticket.arrivalAirport}</h2>
                            <p style="margin:0; color:#666">${new Date(eticket.arrivalTime).toLocaleString([], {month:'short', day:'numeric', hour:'2-digit', minute:'2-digit'})}</p>
                        </div>
                    </div>

                    <div style="background:#f1f5f9; border-radius:8px; padding:15px">
                        <h4 style="margin:0 0 10px 0; color:#475569">Passengers</h4>
                        ${eticket.passengers.map(p => `
                            <div style="display:flex; justify-content:space-between; align-items:center; border-bottom:1px solid #e2e8f0; padding-bottom:10px; margin-bottom:10px">
                                <div>
                                    <h4 style="margin:0; color:#1e293b; font-size:16px">${p.name}</h4>
                                    <p style="margin:0; font-size:11px; color:#64748b; text-transform:uppercase">${p.type} - STATUS: Confirmed</p>
                                </div>
                                <img src="${p.qrCodeBase64}" style="width:80px; height:80px">
                            </div>
                        `).join('')}
                    </div>
                    
                </div>
                <button class="btn" style="margin-top:20px; background:var(--primary)" onclick="window.print()">🖨️ Print Ticket</button>
            </div>
        `;
        showModal('Your E-Ticket');
    } catch(e) {
        alert('Failed to load E-Ticket: ' + e.message);
    }
}

// --- UI Helpers ---
function switchTab(tab) {
    if ((tab === 'companions' || tab === 'bookings' || tab === 'profile' || tab === 'settings' || tab === 'admin') && !API_TOKEN) {
        showAuth();
        return;
    }
    document.querySelectorAll('.view').forEach(v => v.style.display = 'none');
    document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));

    const targetView = document.getElementById(`view-${tab}`);
    if (targetView) targetView.style.display = 'block';

    // Find button by onclick text
    const btns = document.querySelectorAll('.tab-btn');
    btns.forEach(b => { if (b.getAttribute('onclick')?.includes(tab)) b.classList.add('active'); });

    if (tab === 'flights') loadFlights();
    if (tab === 'dashboard') loadDashboard();
    if (tab === 'companions') loadCompanions();
    if (tab === 'bookings') loadMyBookings();
    if (tab === 'profile') loadProfile();
    if (tab === 'settings') loadSettings();
    if (tab === 'admin') {
        loadAdminStats();
        loadAdminBookings();
        loadAdminFlights();
        loadAdminUsers();
    }
}

async function loadDashboard() {
    try {
        const profileRes = await apiFetch('/Users/profile');
        if (profileRes.success) {
            const p = profileRes.data;
            document.getElementById('dash-user-name').innerText = p.name ? p.name.split(' ')[0] : p.userName;
        }

        const companionsRes = await apiFetch('/airline/companions');
        if (companionsRes.success) {
            const companions = companionsRes.data || [];
            document.getElementById('dash-stat-companions').innerText = companions.length;
        }

        const bookingsRes = await apiFetch('/airline/bookings/my-bookings');
        if (bookingsRes.success) {
            const bookings = bookingsRes.data || [];
            document.getElementById('dash-stat-bookings').innerText = bookings.length;

            const upcoming = bookings.filter(b => b.status === 'Confirmed');
            document.getElementById('dash-stat-upcoming').innerText = upcoming.length;

            // Recently booked flights
            const recentList = document.getElementById('dash-recent-bookings');
            recentList.innerHTML = '';
            if (bookings.length === 0) {
                recentList.innerHTML = '<p style="color:var(--text-gray); font-size:14px; text-align:center; padding:20px;">No recent bookings found.</p>';
            } else {
                bookings.slice(0, 3).forEach(b => {
                    recentList.innerHTML += `
                        <div style="display:flex; justify-content:space-between; align-items:center; padding:12px; background:rgba(255,255,255,0.03); border-radius:12px; border:1px solid var(--glass-border);">
                            <div>
                                <h4 style="margin:0; font-size:16px;">${b.fromCode} ➔ ${b.toCode}</h4>
                                <p style="margin:4px 0 0; font-size:12px; color:var(--text-gray)">Booked: ${new Date(b.bookingDate).toLocaleDateString()}</p>
                            </div>
                            <div style="text-align:right;">
                                <span class="status-badge status-${(b.status || 'pending').toLowerCase()}" style="font-size:10px">${b.status || 'Pending'}</span>
                                <p style="margin:4px 0 0; font-size:14px; font-weight:700; color:var(--primary)">$${b.totalPrice}</p>
                            </div>
                        </div>
                    `;
                });
            }
        }
        
        fetchWalletBalance();
    } catch (e) {
        console.error('Failed to load dashboard data', e);
    }
}

async function loadAdminStats() {
    try {
        const res = await apiFetch('/airline/dashboard/stats');
        const s = res.data;
        const container = document.getElementById('admin-stats-summary');
        container.innerHTML = `
            <div class="stat-card">
                <span class="label">Total Revenue</span>
                <span class="value" style="color:var(--success)">$${s.totalRevenue.toLocaleString()}</span>
            </div>
            <div class="stat-card">
                <span class="label">Approved Revenue</span>
                <span class="value" style="color:#22c55e">$${(s.totalRevenue * 0.9).toLocaleString()}</span>
            </div>
            <div class="stat-card">
                <span class="label">Active Flights</span>
                <span class="value">${s.activeFlights}</span>
            </div>
            <div class="stat-card">
                <span class="label">Pending Bookings</span>
                <span class="value" style="color:var(--pending)">${s.pendingBookings}</span>
            </div>
        `;

        // Render Charts if Chart.js is loaded
        if (window.Chart) {
            if (window.revenueChartInstance) window.revenueChartInstance.destroy();
            if (window.statusChartInstance) window.statusChartInstance.destroy();

            window.revenueChartInstance = new Chart(document.getElementById('revenueChart').getContext('2d'), {
                type: 'bar',
                data: {
                    labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
                    datasets: [{
                        label: 'Projected Revenue ($)',
                        data: [10200, 15000, 22000, 0, Math.floor(s.totalRevenue * 0.5), s.totalRevenue],
                        backgroundColor: 'rgba(59, 130, 246, 0.5)',
                        borderColor: '#3b82f6',
                        borderWidth: 1,
                        borderRadius: 4
                    }]
                },
                options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } }
            });

            window.statusChartInstance = new Chart(document.getElementById('statusChart').getContext('2d'), {
                type: 'doughnut',
                data: {
                    labels: ['Confirmed', 'Pending', 'Cancelled'],
                    datasets: [{
                        data: [85, s.pendingBookings, 15],
                        backgroundColor: ['#22c55e', '#f59e0b', '#ef4444'],
                        borderWidth: 0
                    }]
                },
                options: { responsive: true, maintainAspectRatio: false, cutout: '70%'}
            });
        }
    } catch (e) { console.error('Failed to load stats'); }
}

async function loadProfile() {
    const container = document.getElementById('profile-content');
    try {
        container.innerHTML = '<div style="padding:20px; text-align:center">Loading your secure profile...</div>';
        const res = await apiFetch('/Users/profile');
        const p = res.data;

        container.innerHTML = `
            <div style="display:flex; gap:30px; align-items:flex-start">
                <div style="text-align:center">
                    <img src="${p.profilePic || 'https://ui-avatars.com/api/?name=' + p.name + '&size=200&background=6366f1&color=fff'}" 
                         style="width:150px; height:150px; border-radius:30px; object-fit:cover; border:4px solid var(--glass-border); margin-bottom:15px">
                    <div class="status-badge status-${p.status.toLowerCase()}">${p.status}</div>
                </div>
                
                <div style="flex:1">
                    <div style="display:grid; grid-template-columns:1fr 1fr; gap:20px; margin-bottom:20px">
                        <div>
                            <p style="color:var(--text-gray); font-size:12px; margin:0">Full Name</p>
                            <p style="font-size:18px; font-weight:700; margin:5px 0">${p.name}</p>
                        </div>
                        <div>
                            <p style="color:var(--text-gray); font-size:12px; margin:0">Username</p>
                            <p style="font-size:18px; font-weight:700; margin:5px 0">@${p.userName}</p>
                        </div>
                        <div>
                            <p style="color:var(--text-gray); font-size:12px; margin:0">Email Address</p>
                            <p style="font-size:18px; font-weight:700; margin:5px 0">${p.email}</p>
                        </div>
                        <div>
                            <p style="color:var(--text-gray); font-size:12px; margin:0">Member Since</p>
                            <p style="font-size:18px; font-weight:700; margin:5px 0">${new Date(p.createdAt).toLocaleDateString()}</p>
                        </div>
                    </div>

                    <div style="padding:20px; background:rgba(255,255,255,0.05); border-radius:16px; border:1px solid var(--glass-border)">
                        <h4 style="margin-top:0; margin-bottom:15px">Travel Documents</h4>
                        <div style="display:grid; grid-template-columns:1fr 1fr; gap:20px">
                            <div>
                                <p style="color:var(--text-gray); font-size:12px; margin:0">Passport Number</p>
                                <p style="font-size:16px; font-weight:600; margin:5px 0">${p.passportNumber || 'Not set'}</p>
                            </div>
                            <div>
                                <p style="color:var(--text-gray); font-size:12px; margin:0">Nationality</p>
                                <p style="font-size:16px; font-weight:600; margin:5px 0">${p.nationality || 'Not set'}</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    } catch (e) { console.error('Failed to load profile'); }
}

async function loadAdminFlights() {
    if (USER_ROLE !== 'Admin' && USER_ROLE !== 'AirlineCompany') return;
    try {
        const data = await apiFetch('/airline/flights/search', {
            method: 'POST',
            body: JSON.stringify({ pageSize: 100 }) // Load many for management
        });
        const list = document.getElementById('admin-flights-list');
        list.innerHTML = '';
        const flights = data.data.flights || data.data;

        (flights || []).forEach(f => {
            list.innerHTML += `
                <tr style="border-bottom: 1px solid var(--glass-border)">
                    <td style="padding:15px">#${f.flightNumber || f.id}</td>
                    <td>${f.fromCode} ➔ ${f.toCode}</td>
                    <td>${f.airlineName}</td>
                    <td style="color:var(--primary)">${f.createdByUserName || 'System'}</td>
                    <td style="font-size:12px; opacity:0.8">$${f.price} | ${f.availableSeats} Seats</td>
                    <td><span class="status-badge" style="background:#334155">${f.status}</span></td>
                </tr>
            `;
        });
    } catch (e) { console.error('Failed to load admin flights'); }
}

async function showAddFlightForm() {
    try {
        const airlinesData = await apiFetch('/airline/airlines');
        const airlines = airlinesData.data;

        const modalBody = document.getElementById('modal-body');
        modalBody.innerHTML = `
            <div style="max-height: 450px; overflow-y:auto; padding-right:10px">
                <div style="display:grid; grid-template-columns:1fr 1fr; gap:10px">
                    <div>
                        <label style="font-size:11px; color:var(--text-gray)">From (Code):</label>
                        <input type="text" id="af-from" placeholder="e.g. CAI">
                    </div>
                    <div>
                        <label style="font-size:11px; color:var(--text-gray)">To (Code):</label>
                        <input type="text" id="af-to" placeholder="e.g. DXB">
                    </div>
                </div>
                
                <div style="display:grid; grid-template-columns:1fr 1fr; gap:10px">
                    <div>
                        <label style="font-size:11px; color:var(--text-gray)">Departure:</label>
                        <input type="datetime-local" id="af-dept">
                    </div>
                    <div>
                        <label style="font-size:11px; color:var(--text-gray)">Arrival:</label>
                        <input type="datetime-local" id="af-arr">
                    </div>
                </div>

                <div style="display:grid; grid-template-columns:1fr 1fr; gap:10px">
                    <div>
                        <label style="font-size:11px; color:var(--text-gray)">Price ($):</label>
                        <input type="number" id="af-price" placeholder="100.00">
                    </div>
                    <div>
                        <label style="font-size:11px; color:var(--text-gray)">Seats:</label>
                        <input type="number" id="af-seats" placeholder="150">
                    </div>
                </div>

                <label style="font-size:11px; color:var(--text-gray)">Airline:</label>
                <select id="af-airline" style="background:#1e293b; color:white; margin-bottom:15px">
                    ${airlines.map(a => `<option value="${a.id}">${a.name} (${a.country})</option>`).join('')}
                </select>
            </div>
            <button class="btn" style="margin-top:10px" onclick="addFlight()">Create Flight</button>
        `;
        showModal('Add New Flight');
    } catch (e) { alert('Failed to load airlines'); }
}

async function addFlight() {
    const data = {
        departureAirportCode: document.getElementById('af-from').value.toUpperCase(),
        arrivalAirportCode: document.getElementById('af-to').value.toUpperCase(),
        departureTime: document.getElementById('af-dept').value,
        arrivalTime: document.getElementById('af-arr').value,
        price: parseFloat(document.getElementById('af-price').value),
        availableSeats: parseInt(document.getElementById('af-seats').value),
        airlineId: parseInt(document.getElementById('af-airline').value)
    };

    if (!data.departureAirportCode || !data.arrivalAirportCode || !data.departureTime || !data.arrivalTime) {
        alert('Please fill all fields');
        return;
    }

    try {
        await apiFetch('/airline/flights', {
            method: 'POST',
            body: JSON.stringify(data)
        });
        alert('Flight added successfully!');
        closeModal();
        if (document.getElementById('view-admin').style.display !== 'none') {
            loadAdminFlights();
        } else {
            loadFlights();
        }
    } catch (e) { alert(e.message); }
}

async function loadAdminBookings() {
    if (USER_ROLE !== 'Admin' && USER_ROLE !== 'AirlineCompany') return;
    try {
        const data = await apiFetch('/airline/bookings'); // Fixed endpoint
        const list = document.getElementById('admin-bookings-list');
        list.innerHTML = '';
        (data.data || []).forEach(b => {
            list.innerHTML += `
                <tr style="border-bottom: 1px solid var(--glass-border)">
                    <td style="padding:15px">#${b.id}</td>
                    <td>${b.userName || 'User'}</td>
                    <td>${b.fromCode} ➔ ${b.toCode}</td>
                    <td><span class="status-badge status-${(b.status || 'pending').toLowerCase()}">${b.status}</span></td>
                    <td>
                        <button onclick="viewBookingDetails(${b.id})" style="background:var(-- glass-border); border:none; color:white; padding:5px 10px; border-radius:5px; cursor:pointer">Details</button>
                        ${b.status === 'Pending' ? `
                            <button onclick="reviewBooking(${b.id}, 'Approved')" style="background:var(--primary); border:none; color:white; padding:5px 10px; border-radius:5px; cursor:pointer; margin-left:5px">Approve</button>
                            <button onclick="reviewBooking(${b.id}, 'Rejected')" style="background:var(--danger); border:none; color:white; padding:5px 10px; border-radius:5px; cursor:pointer; margin-left:5px">Reject</button>
                        ` : ''}
                    </td>
                </tr>
            `;
        });
    } catch (e) { console.error('Failed to load admin bookings'); }
}

async function viewBookingDetails(id) {
    try {
        const data = await apiFetch(`/airline/bookings/${id}`);
        const b = data.data;
        const modalBody = document.getElementById('modal-body');

        const isAdmin = USER_ROLE === 'Admin' || USER_ROLE === 'AirlineCompany';

        modalBody.innerHTML = `
            <div style="max-height: 400px; overflow-y:auto; padding-right:10px">
                <div style="background:rgba(255,255,255,0.05); padding:15px; border-radius:12px; margin-bottom:20px; border:1px solid var(--glass-border)">
                    <p style="margin-bottom:5px"><strong>Primary Contact:</strong> ${b.userName}</p>
                    <p style="margin-bottom:5px"><strong>Flight:</strong> ${b.fromCode} ➔ ${b.toCode}</p>
                    <p><strong>Total:</strong> $${b.totalPrice} (${b.numberOfSeats} Seats)</p>
                ${b.rejectionReason ? `
                    <p style="margin-top:10px; padding:10px; background:rgba(239,68,68,0.1); border-radius:8px; color:#f87171; font-size:13px"><strong>Rejection Reason:</strong> ${b.rejectionReason}</p>
                    <button class="btn" style="margin-top:10px; background:var(--primary); padding:8px" onclick="openChat(${b.id}, '${b.fromCode} to ${b.toCode}')">💬 ${isAdmin ? 'Chat with Passenger' : 'Contact Airline'}</button>
                ` : ''}
            </div>
                
                <h4 style="margin: 20px 0 15px; display:flex; justify-content:space-between; align-items:center">
                    Passengers Detail
                    <span style="font-size:12px; font-weight:normal; color:var(--text-gray)">List of all travelers</span>
                </h4>

                ${(b.passengers || []).map(p => `
                    <div style="background:rgba(255,255,255,0.05); padding:12px; border-radius:12px; border:1px solid var(--glass-border); display:flex; gap:15px; align-items:center">
                        <img src="${p.profilePic || 'https://ui-avatars.com/api/?name=' + p.firstName + '+' + p.lastName}" style="width:40px; height:40px; border-radius:50%; object-fit:cover; background:var(--glass-border)">
                        <div style="flex:1">
                            <h5 style="margin:0">${p.firstName} ${p.lastName}</h5>
                            <p style="font-size:12px; color:var(--text-gray)">Nation: ${p.nationality || 'N/A'}</p>
                            <p style="font-size:12px; color:var(--text-gray)">Passport: ${p.passportNumber || 'N/A'}</p>
                            <p style="font-size:11px; margin-top:5px"><span class="status-badge" style="background:#334155">${p.ageType}</span> <span class="status-badge status-${(p.status || 'pending').toLowerCase()}">${p.status}</span></p>
                            ${p.status === 'Rejected' && p.rejectionReason ? `<p style="font-size:11px; color:#f87171; margin-top:5px">Reason: ${p.rejectionReason}</p>` : ''}
                        </div>
                        ${isAdmin && p.status === 'Pending' ? `
                            <div style="display:flex; flex-direction:column; gap:5px">
                                <button onclick="reviewPassenger(${p.id}, 'Approved', ${id})" style="background:var(--success); border:none; color:white; padding:5px 12px; border-radius:6px; cursor:pointer; font-size:12px">Approve</button>
                                <button onclick="reviewPassenger(${p.id}, 'Rejected', ${id})" style="background:var(--danger); border:none; color:white; padding:5px 12px; border-radius:6px; cursor:pointer; font-size:12px">Reject</button>
                            </div>
                        ` : ''}
                    </div>
                `).join('')}
            </div>
        `;
        showModal(`Booking Details #${id}`);
    } catch (e) { alert('Failed to load details'); }
}

async function reviewPassenger(passengerId, status, bookingId) {
    let reason = "";
    if (status === 'Rejected') {
        reason = prompt("Please enter the reason for rejection:");
        if (reason === null) return; // Cancelled
    }
    try {
        await apiFetch(`/airline/bookings/passenger/${passengerId}/status?status=${status}&reason=${encodeURIComponent(reason)}`, { method: 'PUT' });
        alert(`Passenger ${status}`);
        viewBookingDetails(bookingId); // Refresh details
    } catch (e) { alert(e.message); }
}

async function reviewBooking(id, status) {
    let reason = "";
    if (status === 'Rejected') {
        reason = prompt("Please enter the reason for rejection:");
        if (reason === null) return; // Cancelled
    }
    try {
        await apiFetch(`/airline/bookings/${id}/status?status=${status}&reason=${encodeURIComponent(reason)}`, { method: 'PUT' });
        alert(`Booking ${status}`);
        loadAdminBookings();
    } catch (e) { alert(e.message); }
}

async function viewFlightBookings(flightId) {
    try {
        const data = await apiFetch(`/airline/bookings/flight/${flightId}/bookings`); // Adjusted to match potential route or just fetch and filter
        // Actually the controller has [HttpGet("flight/{flightId}/bookings")]
        const res = await apiFetch(`/airline/bookings/flight/${flightId}/bookings`);
        const bookings = res.data;
        const modalBody = document.getElementById('modal-body');

        if (!bookings || bookings.length === 0) {
            modalBody.innerHTML = '<p style="padding:20px; text-align:center">No bookings for this flight yet.</p>';
        } else {
            modalBody.innerHTML = `
                <div style="max-height:400px; overflow-y:auto">
                    <table style="width:100%; border-collapse:collapse">
                        <thead style="position:sticky; top:0; background:#1e293b">
                            <tr style="text-align:left; border-bottom:1px solid var(--glass-border)">
                                <th style="padding:10px">ID</th>
                                <th>User</th>
                                <th>Seats</th>
                                <th>Status</th>
                                <th>Action</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${bookings.map(b => `
                                <tr style="border-bottom:1px solid rgba(255,255,255,0.05)">
                                    <td style="padding:10px">#${b.id}</td>
                                    <td>${b.userName}</td>
                                    <td>${b.numberOfSeats}</td>
                                    <td><span class="status-badge status-${(b.status || 'pending').toLowerCase()}">${b.status}</span></td>
                                    <td><button class="btn" style="padding:5px 10px; font-size:12px" onclick="viewBookingDetails(${b.id})">Details</button></td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            `;
        }
        showModal(`Flight Bookings (Flight #${flightId})`);
    } catch (e) { alert('Failed to load bookings for this flight'); }
}

function showModal(title) {
    document.getElementById('modal-title').innerText = title;
    document.getElementById('modal').style.display = 'flex';
}

function closeModal() { document.getElementById('modal').style.display = 'none'; }

async function showETicket(bookingId) {
    try {
        const res = await apiFetch(`/airline/bookings/${bookingId}`);
        const b = res.data;
        const modalBody = document.getElementById('modal-body');

        modalBody.innerHTML = `
            <div class="ticket" id="printable-ticket">
                <div class="ticket-header">
                    <div>
                        <h2 style="margin:0; font-weight:800">BOARDING PASS</h2>
                        <span style="font-size:12px; opacity:0.8">SkyLine Premium Travel</span>
                    </div>
                    <div style="text-align:right">
                        <div style="font-size:24px; font-weight:700">#${b.id}</div>
                    </div>
                </div>
                <div class="ticket-body">
                    <div style="display:flex; justify-content:space-between; margin-bottom:20px">
                        <div>
                            <p style="font-size:10px; color:#64748b; margin:0; text-transform:uppercase">Passenger</p>
                            <p style="font-size:18px; font-weight:700; margin:5px 0">${b.userName}</p>
                        </div>
                        <div style="text-align:right">
                            <p style="font-size:10px; color:#64748b; margin:0; text-transform:uppercase">Class</p>
                            <p style="font-size:18px; font-weight:700; margin:5px 0">Economy</p>
                        </div>
                    </div>

                    <div style="display:grid; grid-template-columns: 1fr auto 1fr; align-items:center; gap:20px; background:#f1f5f9; padding:15px; border-radius:12px; margin-bottom:20px">
                        <div style="text-align:center">
                            <p style="font-size:28px; font-weight:900; margin:0">${b.fromCode}</p>
                            <p style="font-size:11px; color:#64748b; margin:0">DEPARTURE</p>
                        </div>
                        <div style="font-size:20px; color:var(--primary)">✈️</div>
                        <div style="text-align:center">
                            <p style="font-size:28px; font-weight:900; margin:0">${b.toCode}</p>
                            <p style="font-size:11px; color:#64748b; margin:0">ARRIVAL</p>
                        </div>
                    </div>

                    <div style="display:grid; grid-template-columns:1fr 1fr; gap:15px; font-size:14px">
                        <div>
                            <p style="margin:0; color:#64748b">Seats:</p>
                            <p style="font-weight:700; margin:2px 0">${b.numberOfSeats}</p>
                        </div>
                        <div>
                            <p style="margin:0; color:#64748b">Date:</p>
                            <p style="font-weight:700; margin:2px 0">${new Date(b.bookingDate).toLocaleDateString()}</p>
                        </div>
                    </div>
                </div>
                <div class="ticket-footer">
                    <img src="https://api.qrserver.com/v1/create-qr-code/?size=100x100&data=SkyLine-Booking-${b.id}" style="width:80px; margin-bottom:10px">
                    <p style="font-size:10px; color:#94a3b8">Show this QR code at the airport check-in counter.</p>
                    <button class="btn" style="margin-top:15px; width:auto; padding:8px 20px" onclick="window.print()">Print Ticket</button>
                </div>
            </div>
        `;
        showModal('Your E-Ticket');
    } catch (e) { alert('Failed to generate ticket'); }
}

function showAuth() {
    window.location.href = '/auth/login.html';
}

function showRegisterForm() {
    const modalBody = document.getElementById('modal-body');
    modalBody.innerHTML = `
        <div style="max-height: 400px; overflow-y:auto; padding-right:10px">
            <input type="text" id="r-name" placeholder="Full Name">
            <input type="text" id="r-username" placeholder="Username">
            <input type="email" id="r-email" placeholder="Email">
            <input type="password" id="r-pass" placeholder="Password">
            <input type="text" id="r-phone" placeholder="Phone Number">
            <input type="text" id="r-passport" placeholder="Passport Number">
            <input type="text" id="r-nationality" placeholder="Nationality">
            <div style="margin-bottom:10px">
                <label style="font-size:12px; color:var(--text-gray)">Date of Birth:</label>
                <input type="date" id="r-dob">
            </div>
            <select id="r-gender" style="margin-bottom:15px">
                <option value="Male">Male</option>
                <option value="Female">Female</option>
            </select>
        </div>
        <button class="btn" onclick="submitRegister()">Sign Up</button>
        <p style="text-align:center; margin-top:10px; cursor:pointer; color:var(--text-gray)" onclick="showAuth()">Already have an account? Login</p>
    `;
    showModal('Register');
}

function submitRegister() {
    const userData = {
        name: document.getElementById('r-name').value,
        userName: document.getElementById('r-username').value,
        email: document.getElementById('r-email').value,
        password: document.getElementById('r-pass').value,
        phoneNumber: document.getElementById('r-phone').value,
        passportNumber: document.getElementById('r-passport').value,
        nationality: document.getElementById('r-nationality').value,
        dateOfBirth: document.getElementById('r-dob').value,
        gender: document.getElementById('r-gender').value
    };
    register(userData);
}

function showAddCompanionForm() {
    if (!API_TOKEN) { showAuth(); return; }
    const modalBody = document.getElementById('modal-body');
    modalBody.innerHTML = `
        <input type="text" id="c-fname" placeholder="First Name">
        <input type="text" id="c-lname" placeholder="Last Name">
        <select id="c-age" style="background:#1e293b; color:white">
            <option value="Adult">Adult</option>
            <option value="Child">Child</option>
            <option value="Infant">Infant</option>
        </select>
        <input type="text" id="c-pass" placeholder="Passport Number">
        <input type="text" id="c-nat" placeholder="Nationality">
        <input type="text" id="c-profile" placeholder="Profile Picture URL">
        <input type="text" id="c-p-image" placeholder="Passport Image URL">
        <button class="btn" onclick="addCompanion()">Save Companion</button>
    `;
    showModal('Add Companion');
}
let CURRENT_CHAT_BOOKING_ID = null;

async function openChat(bookingId, title) {
    CURRENT_CHAT_BOOKING_ID = bookingId;
    document.getElementById('chat-title').innerText = `Chat: ${title}`;
    document.getElementById('chat-box').style.display = 'flex';
    loadChatHistory();

    // Auto refresh chat every 5 seconds
    if (window.chatInterval) clearInterval(window.chatInterval);
    window.chatInterval = setInterval(loadChatHistory, 5000);
}

function closeChat() {
    document.getElementById('chat-box').style.display = 'none';
    if (window.chatInterval) clearInterval(window.chatInterval);
    CURRENT_CHAT_BOOKING_ID = null;
}

async function loadChatHistory() {
    if (!CURRENT_CHAT_BOOKING_ID) return;
    try {
        const res = await apiFetch(`/airline/chat/${CURRENT_CHAT_BOOKING_ID}`);
        const history = res.data;
        const container = document.getElementById('chat-messages');
        const currentUserId = parseInt(localStorage.getItem('userId')); // Need to store this during login

        container.innerHTML = history.map(m => `
            <div class="chat-msg ${m.senderId === currentUserId ? 'sent' : 'received'}">
                <div style="font-size:10px; opacity:0.7; margin-bottom:3px">${m.senderName}</div>
                ${m.message}
                <div style="font-size:9px; opacity:0.5; text-align:right; margin-top:3px">${new Date(m.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</div>
            </div>
        `).join('');
        container.scrollTop = container.scrollHeight;
    } catch (e) {
        console.error('Chat load error:', e);
    }
}


async function sendMessage() {
    const input = document.getElementById('chat-input');
    const msg = input.value.trim();
    if (!msg || !CURRENT_CHAT_BOOKING_ID) return;

    try {
        await apiFetch('/airline/chat/send', {
            method: 'POST',
            body: JSON.stringify({
                bookingId: CURRENT_CHAT_BOOKING_ID,
                message: msg
            })
        });
        input.value = '';
        loadChatHistory();
    } catch (e) {
        console.error('Send message error:', e);
        alert('Failed to send message: ' + (e.message || 'Unknown error'));
    }
}

// --- Reviews ---
function showAddReviewForm(flightId) {
    if (!API_TOKEN) { showAuth(); return; }
    const modalBody = document.getElementById('modal-body');
    modalBody.innerHTML = `
        <div style="padding:10px">
            <label style="display:block; margin-bottom:10px; color:var(--text-gray); font-size:14px">Rating:</label>
            <div style="display:flex; gap:10px; margin-bottom:20px; font-size:30px">
                <span class="star-rating" data-rating="1" onclick="setRating(1)" style="cursor:pointer; opacity:0.3">⭐</span>
                <span class="star-rating" data-rating="2" onclick="setRating(2)" style="cursor:pointer; opacity:0.3">⭐</span>
                <span class="star-rating" data-rating="3" onclick="setRating(3)" style="cursor:pointer; opacity:0.3">⭐</span>
                <span class="star-rating" data-rating="4" onclick="setRating(4)" style="cursor:pointer; opacity:0.3">⭐</span>
                <span class="star-rating" data-rating="5" onclick="setRating(5)" style="cursor:pointer; opacity:0.3">⭐</span>
            </div>
            <input type="hidden" id="review-rating" value="0">
            <label style="display:block; margin-bottom:5px; color:var(--text-gray); font-size:14px">Your Review:</label>
            <textarea id="review-comment" placeholder="Share your experience with this flight..." style="width:100%; min-height:100px; background:rgba(255,255,255,0.05); border:1px solid var(--glass-border); border-radius:10px; padding:12px; color:white; resize:vertical"></textarea>
            <button class="btn" style="margin-top:15px" onclick="submitReview(${flightId})">Submit Review</button>
        </div>
    `;
    showModal('Rate This Flight');
}

function setRating(rating) {
    document.getElementById('review-rating').value = rating;
    document.querySelectorAll('.star-rating').forEach((star, index) => {
        if (index < rating) {
            star.style.opacity = '1';
        } else {
            star.style.opacity = '0.3';
        }
    });
}

async function submitReview(flightId) {
    const rating = parseInt(document.getElementById('review-rating').value);
    const comment = document.getElementById('review-comment').value.trim();

    if (rating === 0) {
        alert('Please select a rating');
        return;
    }

    try {
        await apiFetch('/airline/reviews', {
            method: 'POST',
            body: JSON.stringify({
                flightId: flightId,
                rating: rating,
                comment: comment
            })
        });
        alert('Thank you for your review!');
        closeModal();
        // Refresh flight details to show new review
        viewFlightDetails(flightId);
    } catch (e) {
        alert('Failed to submit review: ' + e.message);
    }
}

// --- Profile Functions ---
async function loadProfile() {
    try {
        const res = await apiFetch('/Users/profile');
        const user = res.data;

        // Status badge styling
        const statusColors = {
            'Pending': 'background: rgba(251, 191, 36, 0.2); color: #fbbf24',
            'Active': 'background: rgba(16, 185, 129, 0.2); color: #10b981',
            'Suspended': 'background: rgba(239, 68, 68, 0.2); color: #ef4444',
            'Banned': 'background: rgba(127, 29, 29, 0.3); color: #dc2626'
        };

        const statusIcons = {
            'Pending': '⏳',
            'Active': '✅',
            'Suspended': '⚠️',
            'Banned': '🚫'
        };

        const statusStyle = statusColors[user.status] || statusColors['Pending'];
        const statusIcon = statusIcons[user.status] || '⏳';

        document.getElementById('profile-content').innerHTML = `
            <div style="display: flex; gap: 30px; align-items: start">
                <div style="text-align: center">
                    <div style="width: 120px; height: 120px; background: linear-gradient(135deg, var(--primary), var(--primary-hover)); border-radius: 20px; display: flex; align-items: center; justify-content: center; font-size: 48px; font-weight: 800; color: white; margin-bottom: 10px">
                        ${user.name.substring(0, 2).toUpperCase()}
                    </div>
                    <span class="status-badge" style="${statusStyle}; padding: 6px 12px; border-radius: 20px; font-size: 11px; font-weight: 600">
                        ${statusIcon} ${user.status}
                    </span>
                </div>
                <div style="flex: 1">
                    <h3 style="margin: 0 0 5px 0; font-size: 24px">${user.name}</h3>
                    <p style="margin: 0 0 20px 0; color: var(--text-gray)">@${user.userName}</p>
                    
                    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px">
                        <div>
                            <p style="margin: 0; font-size: 12px; color: var(--text-gray)">Email Address</p>
                            <p style="margin: 5px 0; font-weight: 600">📧 ${user.email}</p>
                        </div>
                        <div>
                            <p style="margin: 0; font-size: 12px; color: var(--text-gray)">Phone Number</p>
                            <p style="margin: 5px 0; font-weight: 600">📞 ${user.phoneNumber || 'Not set'}</p>
                        </div>
                        <div>
                            <p style="margin: 0; font-size: 12px; color: var(--text-gray)">Nationality</p>
                            <p style="margin: 5px 0; font-weight: 600">🌍 ${user.nationality || 'Not set'}</p>
                        </div>
                        <div>
                            <p style="margin: 0; font-size: 12px; color: var(--text-gray)">Passport Number</p>
                            <p style="margin: 5px 0; font-weight: 600">🛂 ${user.passportNumber || 'Not set'}</p>
                        </div>
                        <div>
                            <p style="margin: 0; font-size: 12px; color: var(--text-gray)">Role</p>
                            <p style="margin: 5px 0; font-weight: 600">👤 ${user.role}</p>
                        </div>
                        <div>
                            <p style="margin: 0; font-size: 12px; color: var(--text-gray)">Member Since</p>
                            <p style="margin: 5px 0; font-weight: 600">📅 ${new Date(user.createdAt).toLocaleDateString()}</p>
                        </div>
                    </div>
                    
                    ${user.status === 'Pending' ? `
                        <div style="margin-top: 20px; padding: 15px; background: rgba(251, 191, 36, 0.1); border: 1px solid rgba(251, 191, 36, 0.3); border-radius: 12px">
                            <p style="margin: 0; font-size: 14px; color: #fbbf24">
                                ⏳ <strong>Account Pending Approval</strong><br>
                                <span style="font-size: 12px; opacity: 0.9">Your account is awaiting admin approval. You can still browse and use most features.</span>
                            </p>
                        </div>
                    ` : ''}
                </div>
            </div>
        `;
    } catch (e) {
        console.error('Failed to load profile:', e);
        document.getElementById('profile-content').innerHTML = `
            <div style="text-align: center; padding: 50px; color: var(--danger)">
                <p>Failed to load profile. Please try again.</p>
            </div>
        `;
    }
}

// --- Settings Functions ---
async function loadSettings() {
    try {
        const res = await apiFetch('/Users/profile');
        const user = res.data;

        // Fill personal information
        document.getElementById('settings-name').value = user.name || '';
        document.getElementById('settings-username').value = user.userName || '';
        document.getElementById('settings-email').value = user.email || '';
        document.getElementById('settings-phone').value = user.phoneNumber || '';
        document.getElementById('settings-nationality').value = user.nationality || '';
        document.getElementById('settings-passport').value = user.passportNumber || '';

        // Load preferences from localStorage
        document.getElementById('settings-notifications').checked = localStorage.getItem('emailNotifications') === 'true';
        document.getElementById('settings-marketing').checked = localStorage.getItem('marketingEmails') === 'true';
    } catch (e) {
        console.error('Failed to load settings:', e);
        alert('Failed to load settings');
    }
}

async function updateProfile() {
    const updatedData = {
        name: document.getElementById('settings-name').value,
        phoneNumber: document.getElementById('settings-phone').value,
        nationality: document.getElementById('settings-nationality').value,
        passportNumber: document.getElementById('settings-passport').value
    };

    try {
        await apiFetch('/Users/profile', {
            method: 'PUT',
            body: JSON.stringify(updatedData)
        });
        alert('✅ Profile updated successfully!');
        loadProfile(); // Refresh profile view
    } catch (e) {
        alert('Failed to update profile: ' + e.message);
    }
}

async function changePassword() {
    const currentPassword = document.getElementById('settings-current-password').value;
    const newPassword = document.getElementById('settings-new-password').value;
    const confirmPassword = document.getElementById('settings-confirm-password').value;

    if (!currentPassword || !newPassword || !confirmPassword) {
        alert('Please fill all password fields');
        return;
    }

    if (newPassword !== confirmPassword) {
        alert('New passwords do not match!');
        return;
    }

    if (newPassword.length < 6) {
        alert('Password must be at least 6 characters long');
        return;
    }

    try {
        await apiFetch('/Users/change-password', {
            method: 'PUT',
            body: JSON.stringify({
                currentPassword: currentPassword,
                newPassword: newPassword
            })
        });
        alert('✅ Password changed successfully!');
        // Clear password fields
        document.getElementById('settings-current-password').value = '';
        document.getElementById('settings-new-password').value = '';
        document.getElementById('settings-confirm-password').value = '';
    } catch (e) {
        alert('Failed to change password: ' + e.message);
    }
}

function savePreferences() {
    const emailNotifications = document.getElementById('settings-notifications').checked;
    const marketingEmails = document.getElementById('settings-marketing').checked;

    // Save to localStorage (in a real app, this would be saved to backend)
    localStorage.setItem('emailNotifications', emailNotifications);
    localStorage.setItem('marketingEmails', marketingEmails);

    alert('✅ Preferences saved successfully!');
}

// --- User Management Functions (Admin) ---
let ALL_USERS = [];

async function loadAdminUsers() {
    try {
        const res = await apiFetch('/airline/admin/users');
        ALL_USERS = res.data;
        renderUsers(ALL_USERS);
    } catch (e) {
        console.error('Failed to load users:', e);
        document.getElementById('admin-users-list').innerHTML = `
            <tr><td colspan="7" style="text-align:center; padding:20px; color:var(--danger)">
                Failed to load users
            </td></tr>
        `;
    }
}

function renderUsers(users) {
    const statusColors = {
        'Pending': 'background: rgba(251, 191, 36, 0.2); color: #fbbf24',
        'Active': 'background: rgba(16, 185, 129, 0.2); color: #10b981',
        'Suspended': 'background: rgba(239, 68, 68, 0.2); color: #ef4444',
        'Banned': 'background: rgba(127, 29, 29, 0.3); color: #dc2626'
    };

    const statusIcons = {
        'Pending': '⏳',
        'Active': '✅',
        'Suspended': '⚠️',
        'Banned': '🚫'
    };

    document.getElementById('admin-users-list').innerHTML = users.map(u => `
        <tr style="border-bottom: 1px solid var(--glass-border)">
            <td style="padding: 10px">${u.id}</td>
            <td>
                <div style="display: flex; align-items: center; gap: 10px">
                    <div style="width: 35px; height: 35px; background: linear-gradient(135deg, var(--primary), var(--primary-hover)); border-radius: 8px; display: flex; align-items: center; justify-content: center; font-size: 14px; font-weight: 700; color: white">
                        ${u.name.substring(0, 2).toUpperCase()}
                    </div>
                    <div>
                        <div style="font-weight: 600">${u.name}</div>
                        <div style="font-size: 11px; color: var(--text-gray)">@${u.userName}</div>
                    </div>
                </div>
            </td>
            <td style="font-size: 13px">${u.email}</td>
            <td>
                <span style="padding: 4px 10px; border-radius: 12px; font-size: 11px; font-weight: 600; ${u.role === 'Admin' ? 'background: rgba(139, 92, 246, 0.2); color: #8b5cf6' : 'background: rgba(99, 102, 241, 0.2); color: #6366f1'}">
                    ${u.role === 'Admin' ? '👑' : '👤'} ${u.role}
                </span>
            </td>
            <td>
                <span style="padding: 4px 10px; border-radius: 12px; font-size: 11px; font-weight: 600; ${statusColors[u.status]}">
                    ${statusIcons[u.status]} ${u.status}
                </span>
            </td>
            <td style="font-size: 12px; color: var(--text-gray)">
                ${new Date(u.createdAt).toLocaleDateString()}
            </td>
            <td>
                <select 
                    onchange="updateUserStatus(${u.id}, this.value)" 
                    style="padding: 6px 10px; background: rgba(255,255,255,0.05); border: 1px solid var(--glass-border); border-radius: 8px; color: white; font-size: 12px; cursor: pointer"
                    ${u.role === 'Admin' ? 'disabled' : ''}>
                    <option value="">Change Status...</option>
                    <option value="Pending" ${u.status === 'Pending' ? 'selected' : ''}>⏳ Pending</option>
                    <option value="Active" ${u.status === 'Active' ? 'selected' : ''}>✅ Active</option>
                    <option value="Suspended" ${u.status === 'Suspended' ? 'selected' : ''}>⚠️ Suspended</option>
                    <option value="Banned" ${u.status === 'Banned' ? 'selected' : ''}>🚫 Banned</option>
                </select>
            </td>
        </tr>
    `).join('');
}

async function updateUserStatus(userId, newStatus) {
    if (!newStatus) return;

    const confirmMessages = {
        'Pending': 'Set user status to Pending (awaiting approval)?',
        'Active': 'Activate this user account?',
        'Suspended': 'Suspend this user account? They will not be able to access certain features.',
        'Banned': 'Ban this user? This is a severe action and they will be blocked from the system.'
    };

    if (!confirm(confirmMessages[newStatus])) {
        loadAdminUsers(); // Reset dropdown
        return;
    }

    try {
        await apiFetch(`/airline/admin/users/${userId}/status`, {
            method: 'PUT',
            body: JSON.stringify({ status: newStatus })
        });

        alert(`✅ User status updated to ${newStatus} successfully!`);
        loadAdminUsers(); // Refresh list
    } catch (e) {
        alert('Failed to update user status: ' + e.message);
        loadAdminUsers(); // Reset dropdown
    }
}

function filterUsers() {
    const searchTerm = document.getElementById('user-search').value.toLowerCase();
    const statusFilter = document.getElementById('status-filter').value;

    const filtered = ALL_USERS.filter(u => {
        const matchesSearch = u.name.toLowerCase().includes(searchTerm) ||
            u.email.toLowerCase().includes(searchTerm) ||
            u.userName.toLowerCase().includes(searchTerm);

        const matchesStatus = !statusFilter || u.status === statusFilter;

        return matchesSearch && matchesStatus;
    });

    renderUsers(filtered);
}

