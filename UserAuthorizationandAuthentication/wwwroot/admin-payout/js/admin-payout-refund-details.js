const params = new URLSearchParams(window.location.search);
const payoutId = params.get('payoutId');
const itemId = params.get('itemId');

document.addEventListener("DOMContentLoaded", () => {
    const backLink = document.getElementById("back-link");
    if (backLink && payoutId) {
        backLink.href = `admin-payout-details.html?id=${payoutId}`;
    } else if (backLink) {
        backLink.href = "admin-payout.html";
    }

    if (!payoutId || !itemId) {
        showError("Invalid parameters. Payout ID and Item ID are required.");
        return;
    }

    fetchRefundDetails();
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

async function fetchRefundDetails() {
    try {
        const res = await fetch(`/api/admin/payouts/${payoutId}`, { headers: getHeaders() });
        if (res.ok) {
            const raw = await res.json();
            const payoutData = unwrapObject(raw);
            
            const items = unwrapArray(payoutData.items || payoutData.Items || payoutData.payoutItems || payoutData.PayoutItems);
            const item = items.find(i => String(i.id ?? i.Id) === itemId);

            if (!item) {
                showError("Refund item not found in this payout.");
                return;
            }

            renderRefund(payoutData, item);
        } else {
            showError("Failed to load payout details.");
        }
    } catch (err) {
        console.error(err);
        showError("Error connecting to server.");
    }
}

function showError(msg) {
    document.getElementById("refund-details-table").innerHTML = `<tr><td style="text-align:center; color: var(--danger); padding: 40px;">${msg}</td></tr>`;
}

function renderRefund(payout, item) {
    const providerName = payout.providerName ?? payout.ProviderName ?? payout.providerNameSnapshot ?? payout.ProviderNameSnapshot ?? `Provider ID: ${payout.providerId ?? payout.ProviderId ?? 'N/A'}`;
    const weekStart = payout.weekStartDate ?? payout.WeekStartDate;
    const weekEnd = payout.weekEndDate ?? payout.WeekEndDate;
    
    const guestName = item.guestName ?? item.GuestName ?? 'N/A';
    const bId = item.bookingId ?? item.BookingId ?? 'N/A';
    const origPaid = Number(item.originalPaidAmount ?? item.OriginalPaidAmount ?? 0);
    const refundAmt = Number(item.refundAmount ?? item.RefundAmount ?? 0);
    const netAmt = Number(item.netAfterRefundAmount ?? item.NetAfterRefundAmount ?? 0);
    const reason = item.refundReason ?? item.RefundReason ?? 'N/A';
    const ccy = item.currency ?? item.Currency ?? 'USD';

    if (refundAmt === 0) {
        showError("This item has no refund.");
        return;
    }

    const html = `
        <style>
            .info-table td { padding: 12px 16px; border-bottom: 1px solid var(--border); }
            .info-table td:first-child { width: 40%; font-weight: 600; color: var(--text-muted); }
        </style>
        <tr><td>Payout ID</td><td>#${payoutId}</td></tr>
        <tr><td>Payout Week</td><td>${new Date(weekStart).toLocaleDateString()} - ${new Date(weekEnd).toLocaleDateString()}</td></tr>
        <tr><td>Provider</td><td>${providerName}</td></tr>
        <tr><td>Booking ID</td><td>#${bId}</td></tr>
        <tr><td>Guest/User</td><td>${guestName}</td></tr>
        <tr><td>Original Paid Amount</td><td>${origPaid.toLocaleString()} ${ccy}</td></tr>
        <tr><td>Refund Amount</td><td style="color: var(--danger); font-weight: 600;">${refundAmt.toLocaleString()} ${ccy}</td></tr>
        <tr><td>Net After Refund</td><td style="color: var(--primary); font-weight: 600;">${netAmt.toLocaleString()} ${ccy}</td></tr>
        <tr><td>Refund Reason</td><td>${reason}</td></tr>
    `;

    document.getElementById("refund-details-table").innerHTML = html;
}
