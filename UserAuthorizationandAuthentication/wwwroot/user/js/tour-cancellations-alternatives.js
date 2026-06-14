let currentBookingId = null;

// Pay Price Difference Modal State
let currentAltTourId = null;
let currentAltPrice = null;
let currentDifference = null;

document.addEventListener('DOMContentLoaded', () => {
    const urlParams = new URLSearchParams(window.location.search);
    currentBookingId = urlParams.get('bookingId');

    if (!currentBookingId) {
        showError("Invalid Booking ID.");
        return;
    }

    loadAlternatives(currentBookingId);
});

async function loadAlternatives(bookingId) {
    const loader = document.getElementById('page-loader');
    const errorDiv = document.getElementById('page-error');
    const emptyDiv = document.getElementById('page-empty');
    const listDiv = document.getElementById('alternatives-page-list');
    
    loader.style.display = 'flex';
    errorDiv.style.display = 'none';
    emptyDiv.style.display = 'none';
    listDiv.style.display = 'none';
    listDiv.innerHTML = '';

    const response = await API.get(`/users/tour-cancellations/${bookingId}/alternatives`);
    loader.style.display = 'none';

    if (!response.success) {
        errorDiv.style.display = 'block';
        return;
    }

    const alternatives = response.data || [];

    if (alternatives.length === 0) {
        emptyDiv.style.display = 'block';
        return;
    }

    alternatives.forEach(alt => {
        const id = alt.id || alt.Id || alt.altTourId || alt.AltTourId;
        const title = alt.tourTitle || alt.TourTitle || alt.name || alt.Name || 'Alternative Tour';
        const city = alt.city || alt.City || '';
        const dateStr = alt.availableDateTime || alt.AvailableDateTime;
        const originalPrice = Number(alt.originalPrice || alt.OriginalPrice || alt.originalAlternativePrice || alt.OriginalAlternativePrice || 0);
        const finalPrice = Number(alt.finalPrice || alt.FinalPrice || alt.finalPriceAfterDiscount || alt.FinalPriceAfterDiscount || 0);
        const difference = Number(alt.difference || alt.Difference || 0);
        const refundWalletAmount = Number(alt.refundWalletAmount || alt.RefundWalletAmount || 0);
        const payExtraAmount = Number(alt.payExtraAmount || alt.PayExtraAmount || 0);
        const seats = Number(alt.availableSeats !== undefined ? alt.availableSeats : alt.AvailableSeats || 0);

        const formattedDate = dateStr ? new Date(dateStr).toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        }) : 'TBD';

        const isFull = seats <= 0;
        const seatsBadgeClass = isFull ? 'danger-seats' : '';

        let diffBadgeHtml = '';
        let actionText = 'Move Here';
        
        if (payExtraAmount > 0) {
            diffBadgeHtml = `<span style="display:inline-block; margin-top:5px; font-size:0.75rem; padding:3px 6px; background:var(--danger); color:#fff; border-radius:4px; font-weight:600;">Pay Extra: $${payExtraAmount.toFixed(2)}</span>`;
        } else if (refundWalletAmount > 0) {
            diffBadgeHtml = `<span style="display:inline-block; margin-top:5px; font-size:0.75rem; padding:3px 6px; background:var(--accent); color:#fff; border-radius:4px; font-weight:600;">Refund: $${refundWalletAmount.toFixed(2)} to Wallet</span>`;
        } else {
            diffBadgeHtml = `<span style="display:inline-block; margin-top:5px; font-size:0.75rem; padding:3px 6px; background:var(--success); color:#fff; border-radius:4px; font-weight:600;">Even Exchange</span>`;
        }

        const card = document.createElement('div');
        card.className = 'alt-card';
        card.innerHTML = `
            <div class="alt-info">
                <span class="alt-title">${title}</span>
                <span class="tour-city" style="font-size: 0.8rem; margin: 2px 0;"><i class="fas fa-map-marker-alt" style="font-size:0.75rem;"></i> ${city}</span>
                <span style="font-size: 0.85rem; color: var(--text-muted);"><i class="fas fa-calendar-day" style="font-size:0.8rem; color:var(--primary); margin-right:5px;"></i> ${formattedDate}</span>
                <span class="alt-seats ${seatsBadgeClass}">${isFull ? 'FULLY BOOKED' : `${seats} seats left`}</span>
                ${diffBadgeHtml}
            </div>
            <div class="alt-price-btn">
                <div style="display:flex; flex-direction:column; align-items:flex-end;">
                    <span class="alt-price" style="margin-bottom:0;">$${finalPrice.toFixed(2)}</span>
                </div>
                <button onclick="chooseAlternative(${id}, ${payExtraAmount}, ${refundWalletAmount})" class="alt-btn" ${isFull ? 'disabled' : ''} style="margin-top:5px;">${actionText}</button>
            </div>
        `;
        listDiv.appendChild(card);
    });

    listDiv.style.display = 'flex';
}

async function chooseAlternative(altTourId, payExtraAmount, refundWalletAmount) {
    if (!currentBookingId) return;

    if (payExtraAmount > 0) {
        // Open payment modal
        currentAltTourId = altTourId;
        currentAltPrice = 0; // Not strictly needed
        currentDifference = payExtraAmount;
        
        document.getElementById('alt-payment-amount').textContent = `$${payExtraAmount.toFixed(2)}`;
        document.getElementById('alt-payment-modal').style.display = 'flex';
        return;
    }

    // Open confirmation modal for free/refund
    currentAltTourId = altTourId;
    currentDifference = refundWalletAmount > 0 ? -refundWalletAmount : 0;
    
    const refundMsgDiv = document.getElementById('alt-confirm-refund-msg');
    if (refundWalletAmount > 0) {
        document.getElementById('alt-confirm-refund-amount').textContent = `$${refundWalletAmount.toFixed(2)}`;
        refundMsgDiv.style.display = 'block';
    } else {
        refundMsgDiv.style.display = 'none';
    }
    
    document.getElementById('alt-confirm-modal').style.display = 'flex';
}

function closeAltPaymentModal() {
    document.getElementById('alt-payment-modal').style.display = 'none';
    currentAltTourId = null;
    currentAltPrice = null;
    currentDifference = null;
}

function closeAltConfirmModal() {
    document.getElementById('alt-confirm-modal').style.display = 'none';
    currentAltTourId = null;
    currentDifference = null;
}

async function submitAlternativeWithPayment() {
    const paymentMethod = document.querySelector('input[name="altPaymentMethod"]:checked').value;
    processAlternativeSelection(currentAltTourId, paymentMethod);
}

async function submitAlternativeFree() {
    processAlternativeSelection(currentAltTourId, "Wallet");
}

async function processAlternativeSelection(altTourId, paymentMethod) {
    const btnPayment = document.getElementById('confirm-alt-payment-btn');
    const btnFree = document.getElementById('confirm-alt-free-btn');
    
    if (btnPayment) {
        btnPayment.disabled = true;
        if(document.getElementById('alt-payment-modal').style.display === 'flex') {
            btnPayment.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
        }
    }
    if (btnFree) {
        btnFree.disabled = true;
        if(document.getElementById('alt-confirm-modal').style.display === 'flex') {
            btnFree.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
        }
    }

    const response = await API.post(`/users/tour-cancellations/${currentBookingId}/choose-alternative`, {
        alternativeTourId: altTourId,
        paymentMethod: paymentMethod
    });

    if (response.success) {
        if (response.data && response.data.requiresStripePayment && response.data.checkoutUrl) {
            // Redirect to stripe checkout
            window.location.href = response.data.checkoutUrl;
        } else {
            // Successfully processed via wallet (or free)
            window.location.href = 'tour-cancellations.html?success=true';
        }
    } else {
        showError(response.message || 'Failed to process alternative selection');
        if (btnPayment) {
            btnPayment.disabled = false;
            btnPayment.innerHTML = 'Confirm Payment & Move Booking';
        }
        if (btnFree) {
            btnFree.disabled = false;
            btnFree.innerHTML = 'Confirm & Move';
        }
        closeAltPaymentModal();
        closeAltConfirmModal();
    }
}

function showError(message) {
    alert(message);
}
