document.addEventListener("DOMContentLoaded", () => {
    const params = new URLSearchParams(window.location.search);
    const bookingId = params.get('bookingId');
    const payoutId = params.get('payoutId');

    if (!bookingId || !payoutId) {
        showToast("Missing Booking ID or Payout ID.");
        return;
    }

    document.getElementById('back-link').href = `admin-payout-details.html?id=${payoutId}`;
    document.getElementById('booking-id-text').textContent = `Booking #${bookingId}`;

    fetchPayoutDetails(payoutId, bookingId);
});

function getHeaders() {
    const token = localStorage.getItem("token") || sessionStorage.getItem("token") || document.cookie.split('; ').find(row => row.startsWith('token='))?.split('=')[1];
    return {
        "Content-Type": "application/json",
        "Authorization": "Bearer " + token
    };
}

function unwrapObject(raw) {
    return raw?.data ?? raw?.result ?? raw?.item ?? raw ?? {};
}

function unwrapArray(raw) {
    if (Array.isArray(raw)) return raw;
    if (Array.isArray(raw?.items)) return raw.items;
    if (Array.isArray(raw?.data)) return raw.data;
    if (Array.isArray(raw?.data?.items)) return raw.data.items;
    if (Array.isArray(raw?.result)) return raw.result;
    if (Array.isArray(raw?.result?.items)) return raw.result.items;
    return [];
}

async function fetchPayoutDetails(payoutId, bookingId) {
    try {
        const res = await fetch(`/api/admin/payouts/${payoutId}`, { headers: getHeaders() });
        if (res.ok) {
            const raw = await res.json();
            const p = unwrapObject(raw);
            const items = p.items || p.Items || p.payoutItems || p.PayoutItems || p.includedBookings || p.IncludedBookings || [];
            const itemsArray = unwrapArray(items);
            
            const booking = itemsArray.find(b => (b.bookingId ?? b.BookingId) == bookingId);
            if (booking) {
                renderRefundDetails(booking, p.currency ?? p.Currency ?? '');
            } else {
                showToast("Booking not found in this payout batch.");
            }
        } else {
            showToast("Failed to fetch payout details.");
        }
    } catch (err) {
        console.error(err);
        showToast("Error connecting to server.");
    }
}

function safeNumber(value) {
    const n = Number(value ?? 0);
    return Number.isFinite(n) ? n : 0;
}

function formatDate(value) {
    if (!value) return 'N/A';
    const d = new Date(value);
    return Number.isNaN(d.getTime()) ? 'N/A' : d.toLocaleDateString();
}

function renderRefundDetails(booking, currency) {
    const table = document.getElementById("refund-details-table");
    
    const type = booking.bookingType ?? booking.BookingType ?? 'N/A';
    const origPaid = safeNumber(booking.originalPaidAmount ?? booking.OriginalPaidAmount);
    const refundAmt = safeNumber(booking.refundAmount ?? booking.RefundAmount);
    const netAmt = safeNumber(booking.netAfterRefundAmount ?? booking.NetAfterRefundAmount);
    const reason = booking.refundReason ?? booking.RefundReason ?? 'N/A';
    const serviceEnd = booking.serviceEndDate ?? booking.ServiceEndDate;

    table.innerHTML = `
        <tr>
            <td class="info-label">Booking Type</td>
            <td class="info-value">${type}</td>
        </tr>
        <tr>
            <td class="info-label">Service End Date</td>
            <td class="info-value">${formatDate(serviceEnd)}</td>
        </tr>
        <tr>
            <td class="info-label">Original Paid Amount</td>
            <td class="info-value">${origPaid.toLocaleString()} ${currency}</td>
        </tr>
        <tr>
            <td class="info-label" style="color: #ef4444;">Refund Deducted</td>
            <td class="info-value" style="color: #ef4444; font-weight: bold;">-${refundAmt.toLocaleString()} ${currency}</td>
        </tr>
        <tr>
            <td class="info-label">Net After Refund</td>
            <td class="info-value" style="font-weight: bold;">${netAmt.toLocaleString()} ${currency}</td>
        </tr>
        <tr>
            <td class="info-label">Refund Reason</td>
            <td class="info-value" style="white-space: pre-wrap; word-break: break-word;">${reason}</td>
        </tr>
    `;
}

function showToast(message) {
    const toastContainer = document.getElementById("toast-container");
    if(!toastContainer) return;
    const toast = document.createElement("div");
    toast.className = "toast-message";
    toast.innerHTML = `<i class="fa-solid fa-circle-info"></i> ${message}`;
    toastContainer.appendChild(toast);
    
    setTimeout(() => toast.classList.add("active"), 50);
    setTimeout(() => {
        toast.classList.remove("active");
        setTimeout(() => toast.remove(), 300);
    }, 3500);
}
