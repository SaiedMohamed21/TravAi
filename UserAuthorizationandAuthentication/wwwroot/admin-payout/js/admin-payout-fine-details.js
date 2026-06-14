const params = new URLSearchParams(window.location.search);
const payoutId = params.get('payoutId');
const fineId = params.get('fineId');

document.addEventListener("DOMContentLoaded", () => {
    const backLink = document.getElementById("back-link");
    if (backLink && payoutId) {
        backLink.href = `admin-payout-details.html?id=${payoutId}`;
    } else if (backLink) {
        backLink.href = "admin-payout.html";
    }

    if (!payoutId || !fineId) {
        showError("Invalid parameters. Payout ID and Fine ID are required.");
        return;
    }

    fetchFineDetails();
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

async function fetchFineDetails() {
    try {
        const res = await fetch(`/api/admin/payouts/${payoutId}`, { headers: getHeaders() });
        if (res.ok) {
            const raw = await res.json();
            const payoutData = unwrapObject(raw);
            
            const fines = unwrapArray(payoutData.deductions || payoutData.Deductions || payoutData.fineDeductions || payoutData.FineDeductions);
            const fine = fines.find(i => String(i.providerFineId ?? i.ProviderFineId ?? i.fineId ?? i.FineId) === fineId);

            if (!fine) {
                showError("Fine not found in this payout.");
                return;
            }

            renderFine(payoutData, fine);
        } else {
            showError("Failed to load payout details.");
        }
    } catch (err) {
        console.error(err);
        showError("Error connecting to server.");
    }
}

function showError(msg) {
    document.getElementById("fine-details-table").innerHTML = `<tr><td style="text-align:center; color: var(--danger); padding: 40px;">${msg}</td></tr>`;
}

function renderFine(payout, fine) {
    const providerName = payout.providerName ?? payout.ProviderName ?? payout.providerNameSnapshot ?? payout.ProviderNameSnapshot ?? `Provider ID: ${payout.providerId ?? payout.ProviderId ?? 'N/A'}`;
    const weekStart = payout.weekStartDate ?? payout.WeekStartDate;
    const weekEnd = payout.weekEndDate ?? payout.WeekEndDate;
    const ccy = payout.currency ?? payout.Currency ?? 'USD';
    
    const fId = fine.providerFineId ?? fine.ProviderFineId ?? fine.fineId ?? fine.FineId ?? 'N/A';
    const reason = fine.reasonSnapshot ?? fine.ReasonSnapshot ?? 'N/A';
    const amt = Number(fine.amount ?? fine.Amount ?? 0);
    const complaintId = fine.sourceComplaintId ?? fine.SourceComplaintId;

    let complaintRow = '';
    if (complaintId) {
        complaintRow = `<tr><td>Source Complaint</td><td><a href="/hotel/admin-complaint-details.html?id=${complaintId}" class="btn-action btn-outline" style="font-size: 0.8rem; padding: 4px 8px; text-decoration: none; color: var(--primary);">View Complaint #${complaintId}</a></td></tr>`;
    }

    const html = `
        <style>
            .info-table td { padding: 12px 16px; border-bottom: 1px solid var(--border); }
            .info-table td:first-child { width: 40%; font-weight: 600; color: var(--text-muted); }
        </style>
        <tr><td>Payout ID</td><td>#${payoutId}</td></tr>
        <tr><td>Payout Week</td><td>${new Date(weekStart).toLocaleDateString()} - ${new Date(weekEnd).toLocaleDateString()}</td></tr>
        <tr><td>Provider</td><td>${providerName}</td></tr>
        <tr><td>Fine ID</td><td>#${fId}</td></tr>
        <tr><td>Fine Reason</td><td>${reason}</td></tr>
        <tr><td>Fine Amount Deducted</td><td style="color: var(--danger); font-weight: 600;">-${amt.toLocaleString()} ${ccy}</td></tr>
        ${complaintRow}
    `;

    document.getElementById("fine-details-table").innerHTML = html;
}
