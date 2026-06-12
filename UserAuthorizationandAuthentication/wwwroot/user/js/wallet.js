document.addEventListener('DOMContentLoaded', () => {
    // Check auth
    if (!Auth.isLoggedIn()) {
        Auth.logout();
        return;
    }

    // Set user display name
    const user = Auth.getUser();
    const displayNameEl = document.getElementById('user-display-name');
    if (displayNameEl) {
        displayNameEl.textContent = user.fullName || user.username || 'Valued Traveller';
    }

    // Load wallet data
    loadWalletData();

    // Check for success or canceled from Stripe redirect
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.get('success') === 'true') {
        const sessionId = urlParams.get('session_id');
        if (sessionId) {
            confirmWalletTopup(sessionId);
        } else {
            alert('Wallet top-up successful!');
            window.history.replaceState({}, document.title, window.location.pathname);
            loadWalletData();
        }
    } else if (urlParams.get('canceled') === 'true') {
        alert('Wallet top-up was canceled.');
        window.history.replaceState({}, document.title, window.location.pathname);
    }
});

async function loadWalletData() {
    showState('loader');
    
    const balanceRes = await API.get('/airline/wallet/balance');
    const refundsRes = await API.get('/users/wallet/refund-history');
    const txRes = await API.get('/users/wallet/transaction-history');

    if (!balanceRes.success) {
        showError('Could not retrieve wallet balance.', 'API Error');
        return;
    }

    const data = {
        balance: balanceRes.data?.balance !== undefined ? balanceRes.data.balance : 0,
        refunds: refundsRes.data || [],
        transactions: txRes.data || []
    };

    renderWallet(data);
}

function renderWallet(data) {
    if (!data) {
        showError('Retrieved wallet data was empty.', 'No Data');
        return;
    }

    // Render Balance
    const balance = data.balance !== undefined ? data.balance : data.Balance;
    const balanceEl = document.getElementById('wallet-balance');
    if (balanceEl) {
        balanceEl.textContent = Number(balance).toLocaleString('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    // Render Refund History
    const refunds = data.refunds || [];
    const refundsListEl = document.getElementById('refunds-list');
    const refundEmptyEl = document.getElementById('refund-empty-state');
    const refundBadge = document.getElementById('refund-count-badge');

    if (refundBadge) refundBadge.textContent = `${refunds.length} ${refunds.length === 1 ? 'item' : 'items'}`;

    if (refundsListEl) {
        refundsListEl.innerHTML = '';
        if (refunds.length === 0) {
            if (refundEmptyEl) refundEmptyEl.style.display = 'flex';
        } else {
            if (refundEmptyEl) refundEmptyEl.style.display = 'none';
            refunds.forEach(r => {
                refundsListEl.appendChild(createHistoryElement(r, 'refund'));
            });
        }
    }

    // Render Transaction History
    const transactions = data.transactions || [];
    const txListEl = document.getElementById('transactions-list');
    const txEmptyEl = document.getElementById('tx-empty-state');
    const txBadge = document.getElementById('tx-count-badge');

    if (txBadge) txBadge.textContent = `${transactions.length} ${transactions.length === 1 ? 'item' : 'items'}`;

    if (txListEl) {
        txListEl.innerHTML = '';
        if (transactions.length === 0) {
            if (txEmptyEl) txEmptyEl.style.display = 'flex';
        } else {
            if (txEmptyEl) txEmptyEl.style.display = 'none';
            transactions.forEach(t => {
                txListEl.appendChild(createHistoryElement(t, 'transaction'));
            });
        }
    }

    showState('content');
}

function createHistoryElement(item, historyType) {
    const el = document.createElement('div');
    el.className = 'tx-item';

    const amount = Number(item.amount || item.Amount || 0);
    const dateStr = item.date || item.Date || item.createdAt || item.CreatedAt || new Date().toISOString();
    const description = item.description || item.Description || '';
    const status = item.status || item.Status || 'Completed';

    const isPositive = amount >= 0;
    const formattedAmount = `${isPositive ? '+' : ''}$${Math.abs(amount).toLocaleString('en-US', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    })}`;

    const formattedDate = new Date(dateStr).toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });

    let icon = 'fa-receipt';
    let iconClass = 'tx-icon-other';

    if (historyType === 'refund') {
        icon = 'fa-rotate-left';
        iconClass = 'tx-icon-refund';
        const method = item.refundMethod || item.RefundMethod || 'Wallet';
        const type = item.bookingType || item.BookingType || 'Unknown';
        const bookingId = item.bookingId || item.BookingId || '';

        el.innerHTML = `
            <div class="tx-icon-box ${iconClass}"><i class="fas ${icon}"></i></div>
            <div class="tx-info">
                <span class="tx-title">${type} Booking Refund #${bookingId}</span>
                <span class="tx-desc">${formattedDate} &bull; ${method} &bull; <span style="color:var(--success)">${status}</span></span>
                <span style="font-size: 0.8rem; color: var(--text-muted);">${description}</span>
            </div>
            <div class="tx-right">
                <span class="tx-amount tx-amount-positive">${formattedAmount}</span>
            </div>
        `;
    } else {
        icon = 'fa-arrow-down';
        iconClass = 'tx-icon-deposit';
        const method = item.method || item.Method || 'Stripe';
        const refId = item.referenceId || item.ReferenceId || '';

        el.innerHTML = `
            <div class="tx-icon-box ${iconClass}"><i class="fas ${icon}"></i></div>
            <div class="tx-info">
                <span class="tx-title">Wallet Top-up</span>
                <span class="tx-desc">${formattedDate} &bull; ${method} &bull; <span style="color:var(--success)">${status}</span></span>
                ${refId ? `<span style="font-size: 0.8rem; color: var(--text-muted);">Ref: ...${refId.substring(refId.length - 8)}</span>` : ''}
            </div>
            <div class="tx-right">
                <span class="tx-amount tx-amount-positive">${formattedAmount}</span>
            </div>
        `;
    }

    return el;
}

function showState(state) {
    const loader = document.getElementById('loader');
    const error = document.getElementById('error-message');
    const content = document.getElementById('wallet-content');

    if (loader) loader.style.display = state === 'loader' ? 'flex' : 'none';
    if (error) error.style.display = state === 'error' ? 'block' : 'none';
    if (content) content.style.display = state === 'content' ? 'grid' : 'none';
}

function showError(text, title = 'Error') {
    const errorEl = document.getElementById('error-message');
    const errorTitle = document.getElementById('error-title');
    const errorText = document.getElementById('error-text');

    if (errorTitle) errorTitle.textContent = title;
    if (errorText) errorText.textContent = text;
    
    showState('error');
}

// Transaction Details Modal Logic
function closeTxModal() {
    document.getElementById('txModal').style.display = 'none';
}

async function showTransactionDetails(serviceType, bookingId, txAmount) {
    const modal = document.getElementById('txModal');
    const loader = document.getElementById('txModalLoader');
    const content = document.getElementById('txModalContent');
    
    modal.style.display = 'flex';
    loader.style.display = 'flex';
    content.style.display = 'none';
    
    let url = '';
    if (serviceType === 'hotel') url = '/api/user/trips/hotels?tab=cancelled';
    else if (serviceType === 'airline') url = '/api/airline/bookings/my-trips?tab=cancelled';
    else if (serviceType === 'tour') url = '/api/tourguide/bookings/my-trips?tab=cancelled';

    try {
        const response = await API.get(url.replace('/api', '')); // API.get automatically appends /api
        if (!response.success || !response.data) throw new Error('Failed to load trips');
        
        const trips = response.data;
        // Normalize all keys to camelCase for easier access
        const normalize = (obj) => {
            const n = {};
            for (let key in obj) {
                n[key.charAt(0).toLowerCase() + key.slice(1)] = obj[key];
            }
            return n;
        };
        const normalizedTrips = trips.map(normalize);
        
        // Hotel DTO uses bookingId, others use id
        const trip = normalizedTrips.find(t => t.bookingId == bookingId || t.id == bookingId);
        
        if (!trip) {
            content.innerHTML = `<div style="text-align: center; color: var(--text-muted);"><i class="fas fa-exclamation-circle" style="font-size: 2rem; margin-bottom: 10px;"></i><p>Details for this booking could not be found.</p></div>`;
        } else {
            let html = `<div style="display: flex; flex-direction: column; gap: 15px; color: var(--text);">`;
            
            // Header
            html += `<div style="display: flex; justify-content: space-between; align-items: center;">
                        <strong>Booking ID</strong>
                        <span>#${bookingId}</span>
                     </div>
                     <div style="display: flex; justify-content: space-between; align-items: center;">
                        <strong>Service</strong>
                        <span style="text-transform: capitalize;">${serviceType}</span>
                     </div>
                     <hr style="border: none; border-top: 1px dashed var(--border); margin: 5px 0;">`;
            
            // Booking Details
            if (serviceType === 'hotel') {
                html += `<div style="display: flex; justify-content: space-between; align-items: center;">
                            <strong>Hotel Name</strong>
                            <span>${trip.hotelName || 'N/A'}</span>
                         </div>
                         <div style="display: flex; justify-content: space-between; align-items: center;">
                            <strong>Dates</strong>
                            <span>${formatDate(trip.checkInDate)} - ${formatDate(trip.checkOutDate)}</span>
                         </div>`;
            } else if (serviceType === 'airline') {
                html += `<div style="display: flex; justify-content: space-between; align-items: center;">
                            <strong>Flight</strong>
                            <span>${trip.airlineName || ''} ${trip.flightNumber || ''}</span>
                         </div>
                         <div style="display: flex; justify-content: space-between; align-items: center;">
                            <strong>Route</strong>
                            <span>${trip.routeTitle || 'N/A'}</span>
                         </div>`;
            } else if (serviceType === 'tour') {
                html += `<div style="display: flex; justify-content: space-between; align-items: center;">
                            <strong>Tour Name</strong>
                            <span>${trip.tourTitle || 'N/A'}</span>
                         </div>
                         <div style="display: flex; justify-content: space-between; align-items: center;">
                            <strong>Date</strong>
                            <span>${formatDate(trip.tourDate)}</span>
                         </div>`;
            }
            
            // Financial Details
            const totalPaid = trip.totalPrice !== undefined ? parseFloat(trip.totalPrice) : (trip.paidAmount !== undefined ? parseFloat(trip.paidAmount) : 0);
            const refundProcessed = Math.abs(txAmount);
            const cancellationFee = Math.max(0, totalPaid - refundProcessed);

            html += `<hr style="border: none; border-top: 1px dashed var(--border); margin: 5px 0;">
                     <div style="display: flex; justify-content: space-between; align-items: center;">
                        <strong>Total Paid</strong>
                        <span>$${totalPaid.toFixed(2)}</span>
                     </div>
                     <div style="display: flex; justify-content: space-between; align-items: center;">
                        <strong>Cancellation Fee</strong>
                        <span style="color: #f87171;">$${cancellationFee.toFixed(2)}</span>
                     </div>
                     <div style="display: flex; justify-content: space-between; align-items: center; margin-top: 10px; padding-top: 10px; border-top: 1px solid var(--border);">
                        <strong style="color: var(--primary);">Refund Processed</strong>
                        <strong style="color: #34d399;">+$${refundProcessed.toFixed(2)}</strong>
                     </div>`;
            
            html += `</div>`;
            content.innerHTML = html;
        }
    } catch (err) {
        content.innerHTML = `<div style="text-align: center; color: var(--danger);"><i class="fas fa-times-circle" style="font-size: 2rem; margin-bottom: 10px;"></i><p>An error occurred while loading details.</p></div>`;
    }
    
    loader.style.display = 'none';
    content.style.display = 'block';
}

function formatDate(dateStr) {
    if (!dateStr) return 'TBD';
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

async function topupWallet() {
    const amountInput = document.getElementById('topupAmount');
    const amount = parseFloat(amountInput.value);

    if (isNaN(amount) || amount <= 0) {
        alert("Please enter a valid amount greater than 0.");
        return;
    }

    const btn = document.getElementById('topupBtn');
    const originalText = btn.innerHTML;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
    btn.disabled = true;

    try {
        const response = await fetch('/api/users/wallet/topup', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            },
            body: JSON.stringify({ amount: amount })
        });

        const result = await response.json();
        if (response.ok && result.success && result.data && result.data.checkoutUrl) {
            window.location.href = result.data.checkoutUrl;
        } else {
            alert(result.message || "Failed to initiate top-up.");
            btn.innerHTML = originalText;
            btn.disabled = false;
        }
    } catch (error) {
        console.error("Top-up error:", error);
        alert("An error occurred. Please try again.");
        btn.innerHTML = originalText;
        btn.disabled = false;
    }
}

async function confirmWalletTopup(sessionId) {
    try {
        const response = await fetch(`/api/users/wallet/topup/confirm?session_id=${sessionId}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('token')}`
            }
        });
        const result = await response.json();
        if (response.ok && result.success) {
            alert('Wallet top-up confirmed and funds added successfully!');
        } else {
            console.log(result.message);
        }
    } catch (error) {
        console.error("Error confirming top-up:", error);
    } finally {
        window.history.replaceState({}, document.title, window.location.pathname);
        loadWalletData();
    }
}
