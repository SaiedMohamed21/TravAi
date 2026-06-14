let activeBookingId = null;
let pendingAltTourId = null;
let pendingAltExtraAmount = 0;

document.addEventListener('DOMContentLoaded', () => {
    // Check auth
    if (!Auth.isLoggedIn()) {
        Auth.logout();
        return;
    }

    // Check URL parameters for Stripe success/error
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.get('alternativePayment') === 'success') {
        const bookingId = urlParams.get('bookingId');
        const sessionId = urlParams.get('session_id');
        if (bookingId && sessionId) {
            finalizeStripePayment(bookingId, sessionId);
            return; // don't load cancellations yet, wait for finalize
        }
    } else if (urlParams.get('success') === 'true') {
        showToast('Successfully paid the difference and rebooked to alternative tour!', 'success');
        // Clean URL
        window.history.replaceState({}, document.title, window.location.pathname);
    } else if (urlParams.get('error')) {
        alert('Stripe Payment Error: ' + urlParams.get('error'));
        // Clean URL
        window.history.replaceState({}, document.title, window.location.pathname);
    }

    // Load data
    loadCancellations();
});

async function finalizeStripePayment(bookingId, sessionId) {
    showState('loader');
    
    const response = await API.post(`/users/tour-cancellations/${bookingId}/finalize-alternative-stripe`, {
        sessionId: sessionId
    });

    // Clean URL
    window.history.replaceState({}, document.title, window.location.pathname);

    if (response.success) {
        showToast('Successfully finalized alternative booking and payment!', 'success');
    } else {
        alert(`Failed to finalize payment: ${response.message || 'Error occurred'}`);
    }

    // Load data
    loadCancellations();
}

async function loadCancellations() {
    showState('loader');
    
    const response = await API.get('/users/tour-cancellations');

    if (!response.success) {
        showError(response.message || 'Failed to retrieve cancellations.', 'Connection Error');
        return;
    }

    const bookings = response.data || [];
    renderCancellations(bookings);
}

function renderCancellations(bookings) {
    const grid = document.getElementById('resolutions-grid');
    const emptyState = document.getElementById('empty-state');

    if (bookings.length === 0) {
        showState('empty');
        return;
    }

    if (grid) {
        grid.innerHTML = '';
        bookings.forEach(b => {
            const card = createCancellationCard(b);
            grid.appendChild(card);
        });
    }

    showState('content');
}

function createCancellationCard(b) {
    const card = document.createElement('div');
    card.className = 'resolution-card';

    // Normalize property names (handle PascalCase and camelCase)
    const bookingId = b.bookingId || b.BookingId;
    const tourTitle = b.tourTitle || b.TourTitle || 'Unknown Tour';
    const city = b.city || b.City || 'Unknown Destination';
    const tourDate = b.tourDate || b.TourDate;
    const tourTime = b.tourTime || b.TourTime || 'TBD';
    const totalPrice = Number(b.totalPrice || b.TotalPrice || 0);
    const participantsCount = b.participantsCount || b.ParticipantsCount || 1;
    const pricePerPerson = Number(b.pricePerPerson || b.PricePerPerson || 0);
    const reason = b.cancellationReason || b.CancellationReason || 'Tour guide cancelled due to emergency.';
    const breakdownText = `$${pricePerPerson.toFixed(2)} × ${participantsCount} participant${participantsCount > 1 ? 's' : ''} = $${totalPrice.toFixed(2)} total`;

    const formattedDate = tourDate ? new Date(tourDate).toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
    }) : 'TBD';

    const formattedPrice = totalPrice.toLocaleString('en-US', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    card.innerHTML = `
        <div class="card-top">
            <div class="tour-info">
                <h2>${tourTitle}</h2>
                <div class="tour-city"><i class="fas fa-map-marker-alt"></i> ${city}</div>
            </div>
            <div class="price-badge">$${formattedPrice}</div>
        </div>
        <div style="font-size: 0.85rem; color: var(--text-muted); margin: 5px 20px;">
            ${breakdownText}
        </div>

        <div class="details-list">
            <div class="detail-item">
                <i class="fas fa-calendar-alt"></i>
                <span>Date: ${formattedDate} at ${tourTime}</span>
            </div>
            <div class="detail-item">
                <i class="fas fa-ticket-alt"></i>
                <span>Booking ID: #${bookingId}</span>
            </div>
        </div>

        <!-- Cancellation Reason -->
        <div class="alert-reason">
            <i class="fas fa-exclamation-circle"></i>
            <div class="alert-text">
                <h4>Guide Cancellation Reason</h4>
                <p>"${reason}"</p>
            </div>
        </div>

        <!-- Action Buttons -->
        <div class="card-actions">
            <button onclick="openAlternatives(${bookingId})" class="btn-primary">View Alternatives</button>
            <button onclick="confirmRefund(${bookingId})" class="btn-danger-outline">Take Full Refund</button>
        </div>
    `;

    return card;
}

function openAlternatives(bookingId) {
    window.location.href = `tour-cancellations-alternatives.html?bookingId=${bookingId}`;
}

async function confirmRefund(bookingId) {
    activeBookingId = bookingId;
    
    // Open Modal
    const modal = document.getElementById('refund-modal');
    if (modal) modal.style.display = 'flex';
    
    setRefundModalState('loader');

    // Fetch refund preview
    const response = await API.get(`/users/tour-cancellations/${bookingId}/refund-preview`);
    if (!response.success) {
        setRefundModalState('error', response.message || 'Failed to retrieve refund preview.');
        return;
    }

    const preview = response.data;
    if (!preview) {
        setRefundModalState('error', 'No preview data returned.');
        return;
    }

    // Populate modal elements
    document.getElementById('refund-preview-booking-id').textContent = `#${preview.bookingId || bookingId}`;
    document.getElementById('refund-preview-tour-name').textContent = preview.tourName || 'Tour';
    document.getElementById('refund-preview-amount').textContent = `$${Number(preview.refundAmount || 0).toFixed(2)}`;

    const originalAvailable = preview.originalPaymentMethodAvailable;
    const unavailableReason = preview.originalPaymentUnavailableReason;

    const radioOriginal = document.getElementById('radioOriginalPayment');
    const lblOriginal = document.getElementById('lblOriginalPayment');
    const stripeUnavailableSpan = document.getElementById('stripe-unavailable-reason');
    const stripeDescText = document.getElementById('stripe-desc-text');

    // Default to select Wallet
    document.querySelector('input[name="refundMethod"][value="Wallet"]').checked = true;

    if (originalAvailable) {
        if (radioOriginal) radioOriginal.disabled = false;
        if (lblOriginal) {
            lblOriginal.style.opacity = '1';
            lblOriginal.style.cursor = 'pointer';
            lblOriginal.style.pointerEvents = 'auto';
        }
        if (stripeUnavailableSpan) stripeUnavailableSpan.style.display = 'none';
        if (stripeDescText) stripeDescText.style.display = 'block';
    } else {
        if (radioOriginal) radioOriginal.disabled = true;
        if (lblOriginal) {
            lblOriginal.style.opacity = '0.5';
            lblOriginal.style.cursor = 'not-allowed';
            lblOriginal.style.pointerEvents = 'none';
        }
        if (stripeUnavailableSpan) {
            stripeUnavailableSpan.textContent = unavailableReason || 'Stripe refund is unavailable.';
            stripeUnavailableSpan.style.display = 'block';
        }
        if (stripeDescText) stripeDescText.style.display = 'none';
    }

    setRefundModalState('content');
}

function setRefundModalState(state, errorMsg = '') {
    const loader = document.getElementById('refund-modal-loader');
    const error = document.getElementById('refund-modal-error');
    const content = document.getElementById('refund-modal-content');
    const errText = document.getElementById('refund-error-text');

    if (loader) loader.style.display = state === 'loader' ? 'block' : 'none';
    if (error) error.style.display = state === 'error' ? 'block' : 'none';
    if (content) content.style.display = state === 'content' ? 'block' : 'none';
    if (errText && errorMsg) errText.textContent = errorMsg;
}

function closeRefundModal() {
    const modal = document.getElementById('refund-modal');
    if (modal) modal.style.display = 'none';
    activeBookingId = null;
}

async function submitRefund() {
    if (!activeBookingId) return;

    const selectedMethod = document.querySelector('input[name="refundMethod"]:checked').value;
    
    // Disable confirm button and show loading state
    const confirmBtn = document.getElementById('confirm-refund-btn');
    const originalBtnText = confirmBtn ? confirmBtn.textContent : 'Confirm Refund';
    if (confirmBtn) {
        confirmBtn.disabled = true;
        confirmBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing Refund...';
    }

    const response = await API.post(`/users/tour-cancellations/${activeBookingId}/refund`, {
        refundMethod: selectedMethod
    });

    if (confirmBtn) {
        confirmBtn.disabled = false;
        confirmBtn.textContent = originalBtnText;
    }

    if (response.success) {
        showToast('Full refund processed and compensation coupon issued!', 'success');
        closeRefundModal();
        loadCancellations();
    } else {
        alert(`Failed to process refund: ${response.message || 'Error occurred'}`);
    }
}

function closeModal() {
    const modal = document.getElementById('alternatives-modal');
    if (modal) modal.style.display = 'none';
    activeBookingId = null;
}

function showToast(message, type = 'success') {
    const toast = document.getElementById('status-toast');
    if (toast) {
        toast.textContent = message;
        toast.style.borderLeftColor = type === 'success' ? 'var(--accent)' : 'var(--danger)';
        toast.style.display = 'block';
        
        setTimeout(() => {
            toast.style.display = 'none';
        }, 4000);
    }
}

function setModalState(state) {
    const loader = document.getElementById('modal-loader');
    const error = document.getElementById('modal-error');
    const empty = document.getElementById('modal-empty');
    const list = document.getElementById('alternatives-list');

    if (loader) loader.style.display = state === 'loader' ? 'block' : 'none';
    if (error) error.style.display = state === 'error' ? 'block' : 'none';
    if (empty) empty.style.display = state === 'empty' ? 'block' : 'none';
    if (list) list.style.display = state === 'content' ? 'flex' : 'none';
}

function showState(state) {
    const loader = document.getElementById('loader');
    const error = document.getElementById('error-message');
    const empty = document.getElementById('empty-state');
    const grid = document.getElementById('resolutions-grid');

    if (loader) loader.style.display = state === 'loader' ? 'flex' : 'none';
    if (error) error.style.display = state === 'error' ? 'block' : 'none';
    if (empty) empty.style.display = state === 'empty' ? 'block' : 'none';
    if (grid) grid.style.display = state === 'content' ? 'grid' : 'none';
}

function showError(text, title = 'Error') {
    const errorEl = document.getElementById('error-message');
    const errorTitle = document.getElementById('error-title');
    const errorText = document.getElementById('error-text');

    if (errorTitle) errorTitle.textContent = title;
    if (errorText) errorText.textContent = text;
    
    showState('error');
}
