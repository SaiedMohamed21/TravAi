// Admin Payout Details Page Logic

let payoutId = new URLSearchParams(window.location.search).get('id');
let payoutData = null;

document.addEventListener("DOMContentLoaded", () => {
    if (!payoutId) {
        showToast("No Payout ID specified.");
        return;
    }
    
    // Add event listeners to buttons
    document.getElementById("btn-confirm-cancel").addEventListener("click", closeModal);
    document.getElementById("confirm-btn").addEventListener("click", openConfirmModal);
    document.getElementById("btn-confirm-action").addEventListener("click", confirmPayout);
    
    // Check if we have a mark failed button, or create it dynamically later
    // In our UI there's only an export and confirm button currently, we'll append "Mark Failed" dynamically.

    checkStripeRedirect();
    fetchPayoutDetails();
});

async function checkStripeRedirect() {
    const params = new URLSearchParams(window.location.search);
    const payment = params.get('payment');
    const sessionId = params.get('session_id');

    if (payment === 'success' && sessionId) {
        showToast("Verifying Stripe payment...");
        try {
            const res = await fetch(`/api/admin/payouts/${payoutId}/verify-payment?session_id=${sessionId}`, {
                method: 'POST',
                headers: getHeaders()
            });
            const data = await res.json();
            if (res.ok) {
                showToast("Stripe payment verified and payout confirmed!");
                console.log("Stripe Verification Success Checked Values:", data);
                fetchPayoutDetails();
            } else {
                showToast("Failed to verify payment: " + (data.message || "Unknown error"));
                console.error("Stripe Verification Failure Checked Values:", data);
            }
        } catch (err) {
            showToast("Error verifying payment.");
            console.error("Error verifying payment:", err);
        }
        
        // Clean URL
        const url = new URL(window.location);
        url.searchParams.delete('payment');
        url.searchParams.delete('session_id');
        window.history.replaceState({}, document.title, url.toString());
    } else if (payment === 'cancelled') {
        showToast("Payment was cancelled. Payout is still pending.");
        
        const url = new URL(window.location);
        url.searchParams.delete('payment');
        window.history.replaceState({}, document.title, url.toString());
    }
}

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

function safeNumber(value) {
  const n = Number(value ?? 0);
  return Number.isFinite(n) ? n : 0;
}

function formatDate(value) {
  if (!value) return 'N/A';
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? 'N/A' : d.toLocaleDateString();
}

function formatDateTime(value) {
  if (!value) return 'N/A';
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? 'N/A' : d.toLocaleString();
}

async function fetchPayoutDetails() {
    try {
        const res = await fetch(`/api/admin/payouts/${payoutId}`, { headers: getHeaders() });
        if (res.ok) {
            const raw = await res.json();
            payoutData = unwrapObject(raw);
            renderDetails();
        } else {
            showToast("Failed to fetch payout details.");
        }
    } catch (err) {
        console.error(err);
        showToast("Error connecting to server.");
    }
}

function renderDetails() {
    if (!payoutData) return;

    const p = payoutData;
    const batchId = p.id ?? p.payoutBatchId ?? p.PayoutBatchId;
    const providerName = p.providerName ?? p.ProviderName ?? p.providerNameSnapshot ?? p.ProviderNameSnapshot ?? `Provider ID: ${p.providerId ?? p.ProviderId ?? 'N/A'}`;
    const providerType = p.providerType ?? p.ProviderType ?? 'N/A';
    const statusText = p.status ?? p.Status ?? 'N/A';
    const statusClass = String(statusText).toLowerCase();
    const weekStart = p.weekStartDate ?? p.WeekStartDate;
    const weekEnd = p.weekEndDate ?? p.WeekEndDate;
    const generatedAt = p.generatedAt ?? p.GeneratedAt;
    const confirmedAt = p.confirmedAt ?? p.ConfirmedAt;
    
    // Totals
    const grossAmount = safeNumber(p.grossAmount ?? p.GrossAmount);
    const totalRefundAmount = safeNumber(p.totalRefundAmount ?? p.TotalRefundAmount);
    const netAfterRefundAmount = safeNumber(p.netAfterRefundAmount ?? p.NetAfterRefundAmount);
    const totalCommissionAmount = safeNumber(p.totalCommissionAmount ?? p.TotalCommissionAmount);
    const totalFineDeductionAmount = safeNumber(p.totalFineDeductionAmount ?? p.TotalFineDeductionAmount);
    const finalPayoutAmount = safeNumber(p.finalPayoutAmount ?? p.FinalPayoutAmount);
    const ccy = p.currency ?? p.Currency ?? '';

    // Header updates
    document.getElementById("hotel-name-text").textContent = providerName;
    document.querySelector(".modal-title").textContent = `${providerType} Payout Details`;

    // General Details Table
    const detailsTable = document.getElementById("details-table");

    detailsTable.innerHTML = `
        <tr>
            <td><strong>Batch ID</strong></td>
            <td>#${batchId || 'N/A'}</td>
            <td><strong>Status</strong></td>
            <td><span class="status-badge ${statusClass}">${statusText}</span></td>
        </tr>
        <tr>
            <td><strong>Period Start</strong></td>
            <td>${formatDate(weekStart)}</td>
            <td><strong>Generated At</strong></td>
            <td>${formatDateTime(generatedAt)}</td>
        </tr>
        <tr>
            <td><strong>Period End</strong></td>
            <td>${formatDate(weekEnd)}</td>
            <td><strong>Confirmed At</strong></td>
            <td>${formatDateTime(confirmedAt)}</td>
        </tr>
    `;

    // Bookings Table
    const tableBody = document.getElementById("table-body");
    tableBody.innerHTML = "";
    const items = p.items || p.Items || p.payoutItems || p.PayoutItems || p.includedBookings || p.IncludedBookings || [];
    const itemsArray = unwrapArray(items);

    if (itemsArray.length > 0) {
        itemsArray.forEach(item => {
            const itemId = item.bookingId ?? item.BookingId ?? 'N/A';
            const guestName = item.guestName ?? item.GuestName ?? 'N/A';
            const checkIn = item.checkInDate ?? item.CheckInDate;
            const checkOut = item.checkOutDate ?? item.CheckOutDate;
            const itemType = item.bookingType ?? item.BookingType ?? 'N/A';
            const origPaid = safeNumber(item.originalPaidAmount ?? item.OriginalPaidAmount);
            const itemCcy = item.currency ?? item.Currency ?? '';
            const serviceEnd = item.serviceEndDate ?? item.ServiceEndDate;
            const refundAmt = safeNumber(item.refundAmount ?? item.RefundAmount);
            const commPct = safeNumber(item.commissionPercentage ?? item.CommissionPercentage);
            const commAmt = safeNumber(item.commissionAmount ?? item.CommissionAmount);
            const provAmt = safeNumber(item.providerAmount ?? item.ProviderAmount);
            const refundReason = item.refundReason ?? item.RefundReason ?? 'No refund';

            tableBody.innerHTML += `
                <tr>
                    <td>#${itemId}</td>
                    <td>${guestName}</td>
                    <td>${origPaid.toLocaleString()} ${itemCcy}</td>
                    <td>${formatDate(checkIn)}</td>
                    <td>${formatDate(checkOut || serviceEnd)}</td>
                    <td style="color:var(--danger);">${refundAmt > 0 ? '-' + refundAmt.toLocaleString() : '0'} ${itemCcy}</td>
                    <td style="color:var(--danger);">${commAmt > 0 ? '-' + commAmt.toLocaleString() : '0'} ${itemCcy}</td>
                    <td style="color:var(--primary); font-weight:bold;">${provAmt.toLocaleString()} ${itemCcy}</td>
                    <td class="text-right">
                        ${refundAmt > 0 
                            ? `<a href="admin-payout-refund-details.html?payoutId=${payoutId}&itemId=${item.id ?? item.Id}" class="btn-action btn-outline" style="font-size:0.75rem; padding: 4px 8px;">View Refund</a>` 
                            : 'No refund'}
                    </td>
                </tr>
            `;
        });
    } else {
        tableBody.innerHTML = `<tr><td colspan="9" style="text-align:center; color: var(--text-muted); padding: 40px;">No included bookings found for this payout.</td></tr>`;
    }

    document.getElementById("pagination-info").innerHTML = `Showing <strong>${itemsArray.length}</strong> bookings`;

    // Fines Table
    const finesBody = document.getElementById("fines-table-body");
    const finesSection = document.getElementById("fines-section");
    const fines = p.deductions || p.Deductions || p.fineDeductions || p.FineDeductions || [];
    const finesArray = unwrapArray(fines);

    if (finesSection && finesBody) {
        if (finesArray.length > 0) {
            finesSection.style.display = "block";
            finesBody.innerHTML = "";
            finesArray.forEach(fine => {
                const fineId = fine.providerFineId ?? fine.ProviderFineId ?? 'N/A';
                const appliedAt = fine.appliedAt ?? fine.AppliedAt;
                const reason = fine.reasonSnapshot ?? fine.ReasonSnapshot ?? 'N/A';
                const amt = safeNumber(fine.amount ?? fine.Amount);
                
                finesBody.innerHTML += `
                    <tr>
                        <td>#${fineId}</td>
                        <td>${formatDateTime(appliedAt)}</td>
                        <td>${reason}</td>
                        <td style="color:var(--danger); font-weight:bold;">-${amt.toLocaleString()} ${ccy}</td>
                        <td class="text-right">
                            <a href="admin-payout-fine-details.html?payoutId=${payoutId}&fineId=${fineId}" class="btn-action btn-outline" style="font-size:0.75rem; padding: 4px 8px;">View Fine</a>
                        </td>
                    </tr>
                `;
            });
        } else {
            finesSection.style.display = "none";
        }
    }

    // Summary Table
    const summaryTable = document.getElementById("summary-table");
    summaryTable.innerHTML = `
        <tr>
            <td><strong>Gross Bookings Revenue</strong></td>
            <td style="text-align: right;">${grossAmount.toLocaleString()} ${ccy}</td>
        </tr>
        <tr>
            <td><strong>Total Refunds Deducted</strong></td>
            <td style="text-align: right; color: var(--danger);">- ${totalRefundAmount.toLocaleString()} ${ccy}</td>
        </tr>
        <tr>
            <td><strong>Net After Refunds</strong></td>
            <td style="text-align: right; font-weight: 500;">${netAfterRefundAmount.toLocaleString()} ${ccy}</td>
        </tr>
        <tr>
            <td><strong>Total Platform Commission</strong></td>
            <td style="text-align: right; color: var(--danger);">- ${totalCommissionAmount.toLocaleString()} ${ccy}</td>
        </tr>
        <tr>
            <td><strong>Total Fine Deductions</strong></td>
            <td style="text-align: right; color: var(--danger);">- ${totalFineDeductionAmount.toLocaleString()} ${ccy}</td>
        </tr>
        <tr style="background-color: rgba(99, 102, 241, 0.05);">
            <td style="font-size: 1.1rem; font-weight: 600; color: var(--primary);"><strong>Final Payout Amount</strong></td>
            <td style="font-size: 1.2rem; font-weight: 700; color: var(--primary); text-align: right;">
                ${finalPayoutAmount.toLocaleString()} ${ccy}
            </td>
        </tr>
    `;

    // Action buttons logic
    const actionGroup = document.querySelector(".action-group");
    if (statusText === "Pending") {
        document.getElementById("confirm-btn").style.display = "inline-block";
    } else {
        document.getElementById("confirm-btn").style.display = "none";
    }

    if (statusText === "Paid") {
        fetchStripeReceipt();
    }
}

async function fetchStripeReceipt() {
    try {
        const res = await fetch(`/api/admin/payouts/${payoutId}/stripe-payment`, { headers: getHeaders() });
        if (res.ok) {
            const raw = await res.json();
            const receipt = unwrapObject(raw);
            if (receipt && receipt.stripeConnectedAccountId) {
                renderStripeReceipt(receipt);
            }
        }
    } catch (err) {
        console.error("Failed to fetch Stripe receipt", err);
    }
}

function renderStripeReceipt(receipt) {
    let receiptSection = document.getElementById("stripe-receipt-section");
    if (!receiptSection) {
        receiptSection = document.createElement('div');
        receiptSection.id = "stripe-receipt-section";
        document.querySelector(".table-wrapper").after(receiptSection);
    }

    const ccy = (receipt.currency || "").toUpperCase();
    
    let receiptHtml = `
        <h2 class="section-title" style="margin-top: 32px;">Payment Receipt</h2>
        <div class="table-wrapper">
            <table class="custom-table">
                <tbody>
                    <tr>
                        <td><strong>Provider Account</strong></td>
                        <td>${receipt.providerPayoutAccountNumber || 'N/A'}</td>
                        <td><strong>Stripe Connected Account</strong></td>
                        <td>${receipt.stripeConnectedAccountId || 'N/A'}</td>
                    </tr>
                    <tr>
                        <td><strong>Stripe Destination Account</strong></td>
                        <td colspan="3">
                            ${receipt.stripeDestinationAccount ? receipt.stripeDestinationAccount : '<span style="color:var(--danger)">Payment succeeded but no provider destination transfer was found.</span>'}
                        </td>
                    </tr>
                    <tr>
                        <td><strong>Stripe PaymentIntent ID</strong></td>
                        <td>${receipt.stripePaymentIntentId || 'N/A'}</td>
                        <td><strong>Stripe Checkout Session ID</strong></td>
                        <td>${receipt.stripeCheckoutSessionId || 'N/A'}</td>
                    </tr>
                    <tr>
                        <td><strong>Amount Paid</strong></td>
                        <td style="color:var(--primary); font-weight:bold;">${receipt.amount.toLocaleString()} ${ccy}</td>
                        <td><strong>Paid At</strong></td>
                        <td>${formatDateTime(receipt.paidAt)}</td>
                    </tr>
                    <tr>
                        <td><strong>Bank Info</strong></td>
                        <td colspan="3">${receipt.bankName || 'N/A'} (****${receipt.bankLast4 || 'N/A'})</td>
                    </tr>
                </tbody>
            </table>
        </div>
    `;
    receiptSection.innerHTML = receiptHtml;
}

// Modal Controllers
function openConfirmModal() {
    document.getElementById("confirm-modal").classList.add("active");
}

function closeModal() {
    document.getElementById("confirm-modal").classList.remove("active");
}

async function confirmPayout() {
    closeModal();
    const finalAmt = safeNumber(payoutData?.finalPayoutAmount ?? payoutData?.FinalPayoutAmount);
    
    if (finalAmt > 0) {
        // Create Stripe Checkout Session
        showToast("Redirecting to Stripe checkout...");
        try {
            const res = await fetch(`/api/admin/payouts/${payoutId}/create-payment-session`, {
                method: 'POST',
                headers: getHeaders(),
                body: JSON.stringify({})
            });
            const data = await res.json();
            if (res.ok) {
                if (data.skipped) {
                    showToast(data.message);
                    fetchPayoutDetails();
                } else if (data.checkoutUrl) {
                    window.location.href = data.checkoutUrl;
                }
            } else {
                showToast("Failed to create Stripe session: " + (data.message || "Unknown error"));
            }
        } catch (err) {
            showToast("Error connecting to Stripe.");
        }
    } else {
        // If 0 or negative, use the old confirm
        try {
            const res = await fetch(`/api/admin/payouts/${payoutId}/create-payment-session`, {
                method: 'POST',
                headers: getHeaders(),
                body: JSON.stringify({})
            });
            
            if (res.ok) {
                const data = await res.json();
                showToast(data.message || "Payout confirmed successfully!");
                fetchPayoutDetails(); // Refresh
            } else {
                const data = await res.json();
                showToast("Failed to confirm payout: " + (data.message || "Unknown error"));
            }
        } catch(err) {
            showToast("Error connecting to server.");
        }
    }
}

function showToast(message) {
    const toastContainer = document.getElementById("toast-container");
    if(!toastContainer) return;
    const toast = document.createElement("div");
    toast.className = "toast-message";
    toast.innerHTML = `<i class="fa-solid fa-circle-info"></i> ${message}`;
    toastContainer.appendChild(toast);
    
    // Trigger slide-in
    setTimeout(() => toast.classList.add("active"), 50);
    
    // Slide-out and remove
    setTimeout(() => {
        toast.classList.remove("active");
        setTimeout(() => toast.remove(), 300);
    }, 3500);
}

function exportReceipt() {
    window.print();
}
