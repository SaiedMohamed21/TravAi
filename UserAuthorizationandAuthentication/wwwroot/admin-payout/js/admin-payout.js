// Admin Payout Dashboard - Main Page Logic

let ROWS = [];
let PAGE_SIZE = 10;
let currentPage = 1;
let activeTab = "Hotels"; // "Hotels", "Tour Guides", "Airlines"
let searchQuery = "";
let statusFilter = "All Status";
let dateFilter = "All Time";
let sortBy = "Newest";

// Selected payout target for confirmation modal
let payoutTargetId = null;

// DOM Elements
let searchInput, selectStatus, selectDate, selectMonth, selectYear, selectSort, tableBody, paginationInfo, btnPrev, btnNext, tabsContainer, confirmModal, confirmModalMessage, btnConfirmCancel, btnConfirmAction;
let statPendingText, statPaidText, statPendingHotelsText;
let selectStatPending, selectStatPaid, selectStatFailed, selectStatPendingHotels;

document.addEventListener("DOMContentLoaded", () => {
    // Initialize DOM references
    searchInput = document.getElementById("search-input");
    selectStatus = document.getElementById("select-status");
    selectDate = document.getElementById("select-date");
    selectMonth = document.getElementById("select-month");
    selectYear = document.getElementById("select-year");
    selectSort = document.getElementById("select-sort");
    tableBody = document.getElementById("table-body");
    paginationInfo = document.getElementById("pagination-info");
    btnPrev = document.getElementById("btn-prev");
    btnNext = document.getElementById("btn-next");
    tabsContainer = document.getElementById("tabs-container");
    confirmModal = document.getElementById("confirm-modal");
    confirmModalMessage = document.getElementById("confirm-modal-message");
    btnConfirmCancel = document.getElementById("btn-confirm-cancel");
    btnConfirmAction = document.getElementById("btn-confirm-action");
    
    // Summary Cards
    statPendingText = document.getElementById("stat-pending-text");
    statPaidText = document.getElementById("stat-paid-text");
    statPendingHotelsText = document.getElementById("stat-pending-hotels-text");
    selectStatPending = document.getElementById("select-stat-pending");
    selectStatPaid = document.getElementById("select-stat-paid");
    selectStatFailed = document.getElementById("select-stat-failed");
    selectStatPendingHotels = document.getElementById("select-stat-pending-hotels");

    // Event listeners
    if(searchInput) {
        searchInput.addEventListener("input", (e) => {
            searchQuery = e.target.value;
            currentPage = 1;
            render();
        });
    }

    if(selectStatus) {
        selectStatus.addEventListener("change", (e) => {
            statusFilter = e.target.value;
            currentPage = 1;
            render();
        });
    }

    if(selectDate) {
        selectDate.addEventListener("change", (e) => {
            dateFilter = e.target.value;
            if(selectDate.value !== "All Time") {
                if(selectMonth) selectMonth.value = "";
                if(selectYear) selectYear.value = "";
                fetchPayouts(); // re-fetch with empty month/year
            } else {
                currentPage = 1;
                render();
            }
        });
    }

    if(selectMonth) {
        selectMonth.addEventListener("change", () => {
            if(selectDate && selectMonth.value) {
                selectDate.value = "All Time";
                dateFilter = "All Time";
            }
            currentPage = 1;
            fetchPayouts();
        });
    }

    if(selectYear) {
        selectYear.addEventListener("change", () => {
            if(selectDate && selectYear.value) {
                selectDate.value = "All Time";
                dateFilter = "All Time";
            }
            currentPage = 1;
            fetchPayouts();
        });
    }

    if(selectSort) {
        selectSort.addEventListener("change", (e) => {
            sortBy = e.target.value;
            currentPage = 1;
            render();
        });
    }

    // Summary Card Dropdowns
    const summaryDropdowns = [selectStatPending, selectStatPaid, selectStatFailed, selectStatPendingHotels];
    summaryDropdowns.forEach(dropdown => {
        if (dropdown) {
            dropdown.addEventListener("change", updateDynamicSummary);
        }
    });

    const btnResetFilters = document.getElementById("btn-reset-filters");
    if(btnResetFilters) {
        btnResetFilters.addEventListener("click", () => {
            if(searchInput) searchInput.value = "";
            if(selectStatus) selectStatus.value = "All Status";
            if(selectDate) selectDate.value = "All Time";
            if(selectMonth) selectMonth.value = "";
            if(selectYear) selectYear.value = "";
            if(selectSort) selectSort.value = "Newest";
            
            searchQuery = "";
            statusFilter = "All Status";
            dateFilter = "All Time";
            sortBy = "Newest";
            currentPage = 1;
            fetchPayouts();
        });
    }

    if(btnPrev) {
        btnPrev.addEventListener("click", () => {
            if (currentPage > 1) {
                currentPage--;
                render();
            }
        });
    }

    if(btnNext) {
        btnNext.addEventListener("click", () => {
            const totalRows = getFilteredRows().length;
            const totalPages = Math.max(1, Math.ceil(totalRows / PAGE_SIZE));
            if (currentPage < totalPages) {
                currentPage++;
                render();
            }
        });
    }

    // Tab buttons
    if(tabsContainer) {
        const tabButtons = tabsContainer.querySelectorAll(".tab-btn");
        tabButtons.forEach(btn => {
            btn.addEventListener("click", () => {
                tabButtons.forEach(b => b.classList.remove("active"));
                btn.classList.add("active");
                activeTab = btn.getAttribute("data-tab");
                currentPage = 1;
                render();
            });
        });
    }

    // Modal buttons
    if(btnConfirmCancel) btnConfirmCancel.addEventListener("click", closeModal);
    if(btnConfirmAction) btnConfirmAction.addEventListener("click", executePayout);

    // Initial render
    fetchPayouts();
});

function getHeaders() {
    const token = localStorage.getItem("token") || sessionStorage.getItem("token") || document.cookie.split('; ').find(row => row.startsWith('token='))?.split('=')[1];
    return {
        "Content-Type": "application/json",
        "Authorization": "Bearer " + token
    };
}

function safeNumber(value) {
  const n = Number(value ?? 0);
  return Number.isFinite(n) ? n : 0;
}

function money(value) {
  return `${safeNumber(value).toLocaleString()}$`;
}

function isDateOverlap(weekStartStr, weekEndStr, rangeStr) {
    if (rangeStr === "All Time") return true;
    if (!weekStartStr || !weekEndStr) return false;
    const wStart = new Date(weekStartStr);
    const wEnd = new Date(weekEndStr);
    if (Number.isNaN(wStart.getTime()) || Number.isNaN(wEnd.getTime())) return false;

    const now = new Date();
    // Normalize to start of day
    const nowDay = new Date(now.getFullYear(), now.getMonth(), now.getDate());

    let fStart, fEnd;

    if (rangeStr === "Today") {
        fStart = new Date(nowDay);
        fEnd = new Date(nowDay);
    } else if (rangeStr === "This Week") {
        fStart = new Date(nowDay);
        fStart.setDate(fStart.getDate() - 7);
        fEnd = new Date(nowDay);
    } else if (rangeStr === "This Month") {
        fStart = new Date(nowDay.getFullYear(), nowDay.getMonth(), 1);
        fEnd = new Date(nowDay.getFullYear(), nowDay.getMonth() + 1, 0); // last day of month
    } else if (rangeStr === "Last 30 Days") {
        fStart = new Date(nowDay);
        fStart.setDate(fStart.getDate() - 30);
        fEnd = new Date(nowDay);
    } else {
        return true;
    }

    return wEnd >= fStart && wStart <= fEnd;
}

function updateDynamicSummary() {
    let mappedTab = "Hotel";
    if (activeTab === "Tour Guides") mappedTab = "TourGuide";
    if (activeTab === "Airlines") mappedTab = "Airline";

    const rows = Array.isArray(ROWS) ? ROWS : [];
    const typeRows = rows.filter(r => (r.providerType || r.ProviderType) === mappedTab);

    // Get filter values from UI
    const pendingRange = selectStatPending?.value || "This Week";
    const paidRange = selectStatPaid?.value || "This Month";
    const pendingProvidersRange = selectStatPendingHotels?.value || "All Time";

    // Filtering logic for summaries
    const pendingRows = typeRows.filter(r => {
        const status = String(r.status || r.Status || '').toLowerCase();
        if (status !== "pending") return false;
        return isDateOverlap(r.weekStartDate || r.WeekStartDate, r.weekEndDate || r.WeekEndDate, pendingRange);
    });

    const paidRows = typeRows.filter(r => {
        const status = String(r.status || r.Status || '').toLowerCase();
        if (status !== "paid") return false;
        return isDateOverlap(r.weekStartDate || r.WeekStartDate, r.weekEndDate || r.WeekEndDate, paidRange);
    });
    
    const pendingProvidersRows = typeRows.filter(r => {
        const status = String(r.status || r.Status || '').toLowerCase();
        if (status !== "pending") return false;
        return isDateOverlap(r.weekStartDate || r.WeekStartDate, r.weekEndDate || r.WeekEndDate, pendingProvidersRange);
    });

    const sumPending = pendingRows.reduce((acc, r) => acc + safeNumber(r.finalPayoutAmount ?? r.FinalPayoutAmount), 0);
    const sumPaid = paidRows.reduce((acc, r) => acc + safeNumber(r.finalPayoutAmount ?? r.FinalPayoutAmount), 0);
    const pendingProviderIds = new Set(pendingProvidersRows.map(r => r.providerId ?? r.ProviderId));

    if (statPendingText) statPendingText.textContent = money(sumPending);
    if (statPaidText) statPaidText.textContent = money(sumPaid);
    if (statPendingHotelsText) statPendingHotelsText.textContent = `${pendingProviderIds.size}`;

    const sectionTitle = document.querySelector(".section-title");
    if (sectionTitle) {
        if (activeTab === "Hotels") sectionTitle.textContent = "Hotel Payout Management";
        if (activeTab === "Airlines") sectionTitle.textContent = "Airline Payout Management";
        if (activeTab === "Tour Guides") sectionTitle.textContent = "Tour Guide Payout Management";
    }

    const cards = document.querySelectorAll(".stat-card");
    if (cards.length >= 3) {
        const pendingHotelsCardTitle = cards[2].querySelector(".stat-title");
        if (pendingHotelsCardTitle) {
            if (activeTab === "Hotels") pendingHotelsCardTitle.textContent = "Pending Hotels";
            if (activeTab === "Airlines") pendingHotelsCardTitle.textContent = "Pending Airlines";
            if (activeTab === "Tour Guides") pendingHotelsCardTitle.textContent = "Pending Tour Guides";
        }
    }
}

async function fetchPayouts() {
    try {
        tableBody.innerHTML = `<tr><td colspan="11" style="text-align: center; padding: 40px;"><i class="fa-solid fa-spinner fa-spin"></i> Loading...</td></tr>`;
        let url = '/api/admin/payouts';
        let queryParams = [];
        
        const monthVal = document.getElementById("select-month")?.value;
        const yearVal = document.getElementById("select-year")?.value;
        
        if (monthVal) queryParams.push(`month=${monthVal}`);
        if (yearVal) queryParams.push(`year=${yearVal}`);
        
        if (queryParams.length > 0) {
            url += '?' + queryParams.join('&');
        }
        
        const res = await fetch(url, { headers: getHeaders() });
        if (res.ok) {
            const raw = await res.json();
            ROWS = Array.isArray(raw)
              ? raw
              : Array.isArray(raw.items)
                ? raw.items
                : Array.isArray(raw.data)
                  ? raw.data
                  : Array.isArray(raw.data?.items)
                    ? raw.data.items
                    : [];
            render();
        } else {
            tableBody.innerHTML = `<tr><td colspan="11" style="text-align: center; padding: 40px; color: var(--danger);">Failed to load payouts.</td></tr>`;
        }
    } catch (err) {
        console.error(err);
        tableBody.innerHTML = `<tr><td colspan="11" style="text-align: center; padding: 40px; color: var(--danger);">Error connecting to server.</td></tr>`;
    }
}

// Filtering and Sorting
function getFilteredRows() {
    const rows = Array.isArray(ROWS) ? ROWS : [];
    
    let mappedTab = "Hotel";
    if (activeTab === "Tour Guides") mappedTab = "TourGuide";
    if (activeTab === "Airlines") mappedTab = "Airline";

    let filtered = rows.filter(r => (r.providerType || r.ProviderType) === mappedTab);

    if (searchQuery) {
        const q = searchQuery.toLowerCase();
        filtered = filtered.filter(r => {
            const pName = r.providerNameSnapshot || r.ProviderNameSnapshot || "";
            const pId = String(r.providerId ?? r.ProviderId ?? "");
            return pName.toLowerCase().includes(q) || pId.includes(q);
        });
    }

    if (statusFilter !== "All Status") {
        filtered = filtered.filter(r => (r.status || r.Status) === statusFilter);
    }

    // Date filter logic (simple implementation)
    const now = new Date();
    if (dateFilter === "Today") {
        filtered = filtered.filter(r => isDateOverlap(r.weekStartDate || r.WeekStartDate, r.weekEndDate || r.WeekEndDate, "Today"));
    } else if (dateFilter === "This Week") {
        filtered = filtered.filter(r => isDateOverlap(r.weekStartDate || r.WeekStartDate, r.weekEndDate || r.WeekEndDate, "This Week"));
    } else if (dateFilter === "This Month") {
        filtered = filtered.filter(r => isDateOverlap(r.weekStartDate || r.WeekStartDate, r.weekEndDate || r.WeekEndDate, "This Month"));
    }

    filtered.sort((a, b) => {
        const dateA = new Date(a.generatedAt || a.GeneratedAt).getTime();
        const dateB = new Date(b.generatedAt || b.GeneratedAt).getTime();
        const amtA = safeNumber(a.finalPayoutAmount ?? a.FinalPayoutAmount);
        const amtB = safeNumber(b.finalPayoutAmount ?? b.FinalPayoutAmount);
        if (sortBy === "Oldest Due") return dateA - dateB;
        if (sortBy === "Newest") return dateB - dateA;
        if (sortBy === "Highest Amount") return amtB - amtA;
        if (sortBy === "Lowest Amount") return amtA - amtB;
        return 0;
    });

    return filtered;
}

// Render dynamic elements
function render() {
    updateDynamicSummary();
    const filtered = getFilteredRows();
    const totalRows = filtered.length;
    const totalPages = Math.max(1, Math.ceil(totalRows / PAGE_SIZE));
    
    // Bounds check
    if (currentPage > totalPages) {
        currentPage = totalPages;
    }
    if (currentPage < 1) {
        currentPage = 1;
    }
    
    // Slice paged items
    const startIdx = (currentPage - 1) * PAGE_SIZE;
    const paged = filtered.slice(startIdx, startIdx + PAGE_SIZE);

    // Render Table Body
    tableBody.innerHTML = "";
    if (totalRows === 0) {
        const colSpan = 11;
        tableBody.innerHTML = `
            <tr>
                <td colspan="${colSpan}" style="padding: 40px 16px; text-align: center; color: var(--text-muted);">
                    No payouts match your filters.
                </td>
            </tr>
        `;
    } else {
        paged.forEach(r => {
            const providerName = r.providerNameSnapshot || r.ProviderNameSnapshot || 'Provider ' + (r.providerId ?? r.ProviderId);
            const providerType = r.providerType || r.ProviderType || 'N/A';
            const weekStart = r.weekStartDate || r.WeekStartDate;
            const weekEnd = r.weekEndDate || r.WeekEndDate;
            const gross = safeNumber(r.grossAmount ?? r.GrossAmount);
            const refunds = safeNumber(r.totalRefundAmount ?? r.TotalRefundAmount);
            const comm = safeNumber(r.totalCommissionAmount ?? r.TotalCommissionAmount);
            const fines = safeNumber(r.totalFineDeductionAmount ?? r.TotalFineDeductionAmount);
            const finalAmt = safeNumber(r.finalPayoutAmount ?? r.FinalPayoutAmount);
            const currency = r.currency || r.Currency || '';
            const status = r.status || r.Status || 'N/A';
            const generatedAt = r.generatedAt || r.GeneratedAt;
            const payoutId = r.id ?? r.payoutBatchId ?? r.PayoutBatchId;

            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td style="font-weight: 500;">${providerName}</td>
                <td>${providerType}</td>
                <td style="color: var(--text-muted);">${weekStart ? new Date(weekStart).toLocaleDateString() : 'N/A'} - ${weekEnd ? new Date(weekEnd).toLocaleDateString() : 'N/A'}</td>
                <td>${gross.toLocaleString()} ${currency}</td>
                <td style="color: var(--danger);">${refunds > 0 ? '-' + refunds.toLocaleString() : '0'} ${currency}</td>
                <td style="color: var(--danger);">${comm > 0 ? '-' + comm.toLocaleString() : '0'} ${currency}</td>
                <td style="color: var(--danger);">${fines > 0 ? '-' + fines.toLocaleString() : '0'} ${currency}</td>
                <td style="font-weight: bold; color: var(--primary);">${finalAmt.toLocaleString()} ${currency}</td>
                <td>
                    <span class="status-badge ${status.toLowerCase()}">${status}</span>
                </td>
                <td>${generatedAt ? new Date(generatedAt).toLocaleDateString() : 'N/A'}</td>
                <td class="text-right">
                    <div class="action-group">
                        <a href="admin-payout-details.html?id=${payoutId}" class="btn-action btn-outline">
                            <i class="fa-regular fa-eye"></i> View Details
                        </a>
                        ${status === "Pending" ? `
                            <button class="btn-action btn-primary" onclick="openPayoutModal(${payoutId})">
                                <i class="fa-solid fa-check"></i> Confirm
                            </button>
                        ` : ""}
                    </div>
                </td>
            `;
            tableBody.appendChild(tr);
        });
    }

    // Render Pagination Info
    const startNum = totalRows === 0 ? 0 : startIdx + 1;
    const endNum = Math.min(startIdx + PAGE_SIZE, totalRows);
    paginationInfo.innerHTML = `Showing <strong>${startNum}-${endNum}</strong> of <strong>${totalRows}</strong> results`;

    // Toggle pagination disabled status
    btnPrev.disabled = currentPage === 1;
    btnNext.disabled = currentPage === totalPages;
    btnPrev.style.opacity = currentPage === 1 ? "0.5" : "1";
    btnNext.style.opacity = currentPage === totalPages ? "0.5" : "1";
    btnPrev.style.cursor = currentPage === 1 ? "not-allowed" : "pointer";
    btnNext.style.cursor = currentPage === totalPages ? "not-allowed" : "pointer";
}

// Modal Controllers
function openPayoutModal(id) {
    const row = ROWS.find(r => (r.id ?? r.payoutBatchId ?? r.PayoutBatchId) == id);
    if (!row) return;
    payoutTargetId = id;
    const finalAmt = safeNumber(row.finalPayoutAmount ?? row.FinalPayoutAmount);
    const curr = row.currency || row.Currency || '';
    const pName = row.providerNameSnapshot || row.ProviderNameSnapshot || 'Provider ' + (row.providerId ?? row.ProviderId);
    
    confirmModalMessage.innerHTML = `Are you sure you want to confirm a payout of <strong>${finalAmt} ${curr}</strong> for <strong>${pName}</strong>?`;
    confirmModal.classList.add("active");
}

function closeModal() {
    confirmModal.classList.remove("active");
    payoutTargetId = null;
}

async function executePayout() {
    const id = payoutTargetId;
    closeModal();
    if (!id) return;
    
    showToast("Processing payment session...");
    try {
        const res = await fetch(`/api/admin/payouts/${id}/create-payment-session`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({})
        });
        
        const data = await res.json();
        if (res.ok) {
            if (data.skipped) {
                showToast(data.message || "Payout confirmed successfully!");
                fetchPayouts();
            } else if (data.checkoutUrl) {
                window.location.href = data.checkoutUrl;
            }
        } else {
            showToast("Failed to create Stripe session: " + (data.message || "Unknown error"));
        }
    } catch(err) {
        showToast("Error connecting to server.");
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

// Generate Weekly Payouts API Call
async function generateWeeklyPayouts() {
    const btn = document.getElementById("btn-generate-payouts");
    const originalText = btn.innerHTML;
    btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Generating...';
    btn.disabled = true;

    try {
        const response = await fetch('/api/admin/payouts/generate-weekly', {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({}) // Default: generate all types
        });

        const result = await response.json();
        
        if (response.ok && result.success) {
            showToast(result.message || "Successfully generated weekly payouts.");
            fetchPayouts();
        } else {
            showToast("Failed to generate payouts: " + (result.message || "Unknown error"));
        }
    } catch (error) {
        console.error("Error generating payouts:", error);
        showToast("Error connecting to server.");
    } finally {
        btn.innerHTML = originalText;
        btn.disabled = false;
    }
}
