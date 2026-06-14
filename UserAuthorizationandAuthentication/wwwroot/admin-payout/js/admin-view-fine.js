document.addEventListener("DOMContentLoaded", () => {
    const params = new URLSearchParams(window.location.search);
    const fineId = params.get('fineId');
    const payoutId = params.get('payoutId');

    if (!fineId || !payoutId) {
        showToast("Missing Fine ID or Payout ID.");
        return;
    }

    document.getElementById('back-link').href = `admin-payout-details.html?id=${payoutId}`;
    document.getElementById('fine-id-text').textContent = `Fine #${fineId}`;

    fetchPayoutDetails(payoutId, fineId);
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

async function fetchPayoutDetails(payoutId, fineId) {
    try {
        const res = await fetch(`/api/admin/payouts/${payoutId}`, { headers: getHeaders() });
        if (res.ok) {
            const raw = await res.json();
            const p = unwrapObject(raw);
            const fines = p.fineDeductions || p.FineDeductions || p.fines || p.Fines || p.appliedFines || p.AppliedFines || [];
            const finesArray = unwrapArray(fines);
            
            const fine = finesArray.find(d => (d.providerFineId ?? d.ProviderFineId) == fineId);
            if (fine) {
                renderFineDetails(fine, p.currency ?? p.Currency ?? '');
            } else {
                showToast("Fine not found in this payout batch.");
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

function renderFineDetails(fine, currency) {
    const table = document.getElementById("fine-details-table");
    
    const fAmt = safeNumber(fine.amount ?? fine.Amount);
    const reason = fine.reasonSnapshot ?? fine.ReasonSnapshot ?? 'N/A';
    const fineCreated = fine.fineCreatedAt ?? fine.FineCreatedAt;
    const appliedAt = fine.appliedAt ?? fine.AppliedAt;

    table.innerHTML = `
        <tr>
            <td class="info-label">Fine Created At</td>
            <td class="info-value">${formatDate(fineCreated)}</td>
        </tr>
        <tr>
            <td class="info-label">Applied to Payout At</td>
            <td class="info-value">${formatDate(appliedAt)}</td>
        </tr>
        <tr>
            <td class="info-label" style="color: #ef4444;">Fine Amount Deducted</td>
            <td class="info-value" style="color: #ef4444; font-weight: bold;">-${fAmt.toLocaleString()} ${currency}</td>
        </tr>
        <tr>
            <td class="info-label">Fine Reason</td>
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
