// Admin Payout Dashboard - Main Page Logic

// Mock Payouts Dataset (based on reference neat-payout-board-main)
const ROWS = [
    { id: "p1", hotel: "Hilton Hotel & Resort", amount: "4,500$", amountValue: 4500, dueSince: "14 days ago", dueDays: 14, status: "Pending" },
    { id: "p2", hotel: "Marriott Premium Suites", amount: "3,200$", amountValue: 3200, dueSince: "7 days ago", dueDays: 7, status: "Pending" },
    { id: "p3", hotel: "Four Seasons Luxury", amount: "8,750$", amountValue: 8750, dueSince: "3 days ago", dueDays: 3, status: "Paid" },
    { id: "p4", hotel: "Ritz-Carlton Plaza", amount: "2,100$", amountValue: 2100, dueSince: "21 days ago", dueDays: 21, status: "Pending" },
    { id: "p5", hotel: "Hyatt Regency Heights", amount: "5,400$", amountValue: 5400, dueSince: "5 days ago", dueDays: 5, status: "Pending" },
    { id: "p6", hotel: "Sheraton Grand Hotel", amount: "1,900$", amountValue: 1900, dueSince: "10 days ago", dueDays: 10, status: "Paid" },
    { id: "p7", hotel: "InterContinental Cairo", amount: "1,500$", amountValue: 1500, dueSince: "2 days ago", dueDays: 2, status: "Failed" },
    { id: "p8", hotel: "Kempinski Nile Hotel", amount: "2,800$", amountValue: 2800, dueSince: "12 days ago", dueDays: 12, status: "Failed" }
];

const RANGE_DAYS = {
    "Today": 1,
    "This Week": 7,
    "This Month": 30,
    "Last 30 Days": 30,
    "All Time": 99999
};

// State Variables
let activeTab = "Hotels";
let searchQuery = "";
let statusFilter = "All Status";
let dateFilter = "This Month";
let sortBy = "Oldest Due";
let currentPage = 1;
const PAGE_SIZE = 5;

// Selected payout target for confirmation modal
let payoutTargetId = null;

// DOM Elements
let searchInput, selectStatus, selectDate, selectSort, tableBody, paginationInfo, btnPrev, btnNext, tabsContainer, confirmModal, confirmModalMessage, btnConfirmCancel, btnConfirmAction;
let statPendingText, statPaidText, statFailedText, statPendingHotelsText;

document.addEventListener("DOMContentLoaded", () => {
    // Initialize DOM references
    searchInput = document.getElementById("search-input");
    selectStatus = document.getElementById("select-status");
    selectDate = document.getElementById("select-date");
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
    statFailedText = document.getElementById("stat-failed-text");
    statPendingHotelsText = document.getElementById("stat-pending-hotels-text");

    // Event listeners
    searchInput.addEventListener("input", (e) => {
        searchQuery = e.target.value;
        currentPage = 1;
        render();
    });

    selectStatus.addEventListener("change", (e) => {
        statusFilter = e.target.value;
        currentPage = 1;
        render();
    });

    selectDate.addEventListener("change", (e) => {
        dateFilter = e.target.value;
        currentPage = 1;
        render();
    });

    selectSort.addEventListener("change", (e) => {
        sortBy = e.target.value;
        currentPage = 1;
        render();
    });

    btnPrev.addEventListener("click", () => {
        if (currentPage > 1) {
            currentPage--;
            render();
        }
    });

    btnNext.addEventListener("click", () => {
        const totalRows = getFilteredRows().length;
        const totalPages = Math.max(1, Math.ceil(totalRows / PAGE_SIZE));
        if (currentPage < totalPages) {
            currentPage++;
            render();
        }
    });

    // Tab buttons
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

    // Modal buttons
    btnConfirmCancel.addEventListener("click", closeModal);
    btnConfirmAction.addEventListener("click", executePayout);

    // Initial render
    updateStats("This Week", "This Month", "All Time", "All Time");
    render();
});

// Update stats based on dropdown ranges
function updateStats(pendingRange, paidRange, failedRange, pendingHotelsRange) {
    const rangeRows = (range) => ROWS.filter(r => r.dueDays <= (RANGE_DAYS[range] || 99999));
    
    const sumPending = rangeRows(pendingRange).filter(r => r.status === "Pending").reduce((s, r) => s + r.amountValue, 0);
    const sumPaid = rangeRows(paidRange).filter(r => r.status === "Paid").reduce((s, r) => s + r.amountValue, 0);
    const sumFailed = rangeRows(failedRange).filter(r => r.status === "Failed").reduce((s, r) => s + r.amountValue, 0);
    
    // Count unique hotel names that are pending
    const pendingHotels = new Set(
        rangeRows(pendingHotelsRange)
            .filter(r => r.status === "Pending")
            .map(r => r.hotel)
    );
    const countPendingHotels = pendingHotels.size;

    statPendingText.textContent = `${sumPending.toLocaleString()}$`;
    statPaidText.textContent = `${sumPaid.toLocaleString()}$`;
    statFailedText.textContent = `${sumFailed.toLocaleString()}$`;
    statPendingHotelsText.textContent = String(countPendingHotels);
}

// Event handlers for stats dropdowns
function onPendingRangeChange(e) {
    updateStats(
        e.target.value,
        document.getElementById("select-stat-paid").value,
        document.getElementById("select-stat-failed").value,
        document.getElementById("select-stat-pending-hotels").value
    );
}
function onPaidRangeChange(e) {
    updateStats(
        document.getElementById("select-stat-pending").value,
        e.target.value,
        document.getElementById("select-stat-failed").value,
        document.getElementById("select-stat-pending-hotels").value
    );
}
function onFailedRangeChange(e) {
    updateStats(
        document.getElementById("select-stat-pending").value,
        document.getElementById("select-stat-paid").value,
        e.target.value,
        document.getElementById("select-stat-pending-hotels").value
    );
}
function onPendingHotelsRangeChange(e) {
    updateStats(
        document.getElementById("select-stat-pending").value,
        document.getElementById("select-stat-paid").value,
        document.getElementById("select-stat-failed").value,
        e.target.value
    );
}

// Filtering and Sorting
function getFilteredRows() {
    // If Airlines or Tour Guides tab is selected, return empty placeholder list for now
    if (activeTab !== "Hotels") {
        return [];
    }

    return ROWS
        .filter(r => r.hotel.toLowerCase().includes(searchQuery.toLowerCase()))
        .filter(r => statusFilter === "All Status" || r.status === statusFilter)
        .filter(r => r.dueDays <= (RANGE_DAYS[dateFilter] || 99999))
        .sort((a, b) => {
            switch (sortBy) {
                case "Oldest Due": return b.dueDays - a.dueDays;
                case "Newest": return a.dueDays - b.dueDays;
                case "Highest Amount": return b.amountValue - a.amountValue;
                case "Lowest Amount": return a.amountValue - b.amountValue;
                default: return 0;
            }
        });
}

// Render dynamic elements
function render() {
    const filtered = getFilteredRows();
    const totalRows = filtered.length;
    const totalPages = Math.max(1, Math.ceil(totalRows / PAGE_SIZE));
    
    // Bounds check
    if (currentPage > totalPages) {
        currentPage = totalPages;
    }
    
    // Slice paged items
    const startIdx = (currentPage - 1) * PAGE_SIZE;
    const paged = filtered.slice(startIdx, startIdx + PAGE_SIZE);

    // Render Table Body
    tableBody.innerHTML = "";
    if (totalRows === 0) {
        const colSpan = 5;
        tableBody.innerHTML = `
            <tr>
                <td colspan="${colSpan}" style="padding: 40px 16px; text-align: center; color: var(--text-muted);">
                    ${activeTab === "Hotels" ? "No payouts match your filters." : `No active payouts available for ${activeTab}.`}
                </td>
            </tr>
        `;
    } else {
        paged.forEach(r => {
            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td style="font-weight: 500;">${r.hotel}</td>
                <td>${r.amount}</td>
                <td style="color: var(--text-muted);">${r.dueSince}</td>
                <td>
                    <span class="status-badge ${r.status.toLowerCase()}">${r.status}</span>
                </td>
                <td class="text-right">
                    <div class="action-group">
                        <a href="admin-payout-details.html?id=${r.id}" class="btn-action btn-outline">
                            <i class="fa-regular fa-eye"></i> View Details
                        </a>
                        ${r.status !== "Paid" ? `
                            <button class="btn-action btn-primary" onclick="openPayoutModal('${r.id}')">
                                <i class="fa-solid fa-plus"></i> Create Payout
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
    const row = ROWS.find(r => r.id === id);
    if (!row) return;
    payoutTargetId = id;
    confirmModalMessage.innerHTML = `Are you sure you want to create a payout of <strong>${row.amount}</strong> for <strong>${row.hotel}</strong>? Please confirm to proceed.`;
    confirmModal.classList.add("active");
}

function closeModal() {
    confirmModal.classList.remove("active");
    payoutTargetId = null;
}

function executePayout() {
    const row = ROWS.find(r => r.id === payoutTargetId);
    if (row) {
        // Update mock row status
        row.status = "Paid";
        row.dueSince = "Paid today";
        row.dueDays = 0;
        
        // Show success toast
        showToast(`Payout of ${row.amount} for ${row.hotel} created successfully.`);
        
        // Update stats and re-render
        updateStats(
            document.getElementById("select-stat-pending").value,
            document.getElementById("select-stat-paid").value,
            document.getElementById("select-stat-failed").value,
            document.getElementById("select-stat-pending-hotels").value
        );
        render();
    }
    closeModal();
}

function showToast(message) {
    const toastContainer = document.getElementById("toast-container");
    const toast = document.createElement("div");
    toast.className = "toast-message";
    toast.innerHTML = `<i class="fa-solid fa-circle-check"></i> ${message}`;
    toastContainer.appendChild(toast);
    
    // Trigger slide-in
    setTimeout(() => toast.classList.add("active"), 50);
    
    // Slide-out and remove
    setTimeout(() => {
        toast.classList.remove("active");
        setTimeout(() => toast.remove(), 300);
    }, 3500);
}
