// Admin Payout Details - Sub-page Logic

// Mock Payouts Dataset
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

// Mock Bookings Receipts included in the payout (recreated from payouts.ts)
const RECEIPTS = [
    { id: "BK-1042", customer: "Sarah Johnson", booked: "Apr 28, 2026", checkIn: "May 02, 2026", checkOut: "May 05, 2026", total: "$650", fee: "$65", net: "$585", rooms: 1, roomType: "Deluxe Room", bedType: "King Bed", nights: 3, pricePerNight: "$216", totalRoomPrice: "$650", paymentMethod: "Visa", paymentDate: "Apr 28, 2026", paymentStatus: "Settled", bookingStatus: "Completed" },
    { id: "BK-1058", customer: "David Lee", booked: "Apr 30, 2026", checkIn: "May 04, 2026", checkOut: "May 06, 2026", total: "$420", fee: "$42", net: "$378", rooms: 1, roomType: "Standard Room", bedType: "Queen Bed", nights: 2, pricePerNight: "$210", totalRoomPrice: "$420", paymentMethod: "Mastercard", paymentDate: "Apr 30, 2026", paymentStatus: "Settled", bookingStatus: "Completed" },
    { id: "BK-1071", customer: "Emma Williams", booked: "May 01, 2026", checkIn: "May 05, 2026", checkOut: "May 09, 2026", total: "$890", fee: "$89", net: "$801", rooms: 2, roomType: "Suite", bedType: "King Bed", nights: 4, pricePerNight: "$222", totalRoomPrice: "$890", paymentMethod: "Visa", paymentDate: "May 01, 2026", paymentStatus: "Settled", bookingStatus: "Completed" },
    { id: "BK-1083", customer: "Liam Brown", booked: "May 02, 2026", checkIn: "May 06, 2026", checkOut: "May 07, 2026", total: "$310", fee: "$31", net: "$279", rooms: 1, roomType: "Standard Room", bedType: "Twin Bed", nights: 1, pricePerNight: "$310", totalRoomPrice: "$310", paymentMethod: "Mastercard", paymentDate: "May 02, 2026", paymentStatus: "Settled", bookingStatus: "Completed" },
    { id: "BK-1090", customer: "Olivia Davis", booked: "May 03, 2026", checkIn: "May 07, 2026", checkOut: "May 10, 2026", total: "$540", fee: "$54", net: "$486", rooms: 1, roomType: "Deluxe Room", bedType: "Queen Bed", nights: 3, pricePerNight: "$180", totalRoomPrice: "$540", paymentMethod: "Visa", paymentDate: "May 03, 2026", paymentStatus: "Settled", bookingStatus: "Completed" }
];

let selectedRow = null;
let currentPage = 1;
const PAGE_SIZE = 5;

// DOM references
let hotelNameText, tableBody, paginationInfo, btnPrev, btnNext, detailsTable, confirmBtn, confirmModal, btnConfirmCancel, btnConfirmAction, receiptModal, receiptModalBody, btnCloseReceipt;

document.addEventListener("DOMContentLoaded", () => {
    // 1. Get query parameters
    const params = new URLSearchParams(window.location.search);
    const id = params.get("id") || "p1";
    selectedRow = ROWS.find(r => r.id === id) || ROWS[0];

    // DOM bindings
    hotelNameText = document.getElementById("hotel-name-text");
    tableBody = document.getElementById("table-body");
    paginationInfo = document.getElementById("pagination-info");
    btnPrev = document.getElementById("btn-prev");
    btnNext = document.getElementById("btn-next");
    detailsTable = document.getElementById("details-table");
    confirmBtn = document.getElementById("confirm-btn");
    
    // Modals
    confirmModal = document.getElementById("confirm-modal");
    btnConfirmCancel = document.getElementById("btn-confirm-cancel");
    btnConfirmAction = document.getElementById("btn-confirm-action");
    receiptModal = document.getElementById("receipt-modal");
    receiptModalBody = document.getElementById("receipt-modal-body");
    btnCloseReceipt = document.getElementById("btn-close-receipt");

    // Dynamic Title
    hotelNameText.textContent = selectedRow.hotel;

    // Confirm button click
    if (selectedRow.status === "Paid") {
        confirmBtn.style.display = "none";
    }
    confirmBtn.addEventListener("click", () => {
        confirmModal.classList.add("active");
    });

    btnConfirmCancel.addEventListener("click", () => {
        confirmModal.classList.remove("active");
    });

    btnConfirmAction.addEventListener("click", executeConfirmPayout);
    
    // Receipt Modal controls
    btnCloseReceipt.addEventListener("click", () => {
        receiptModal.classList.remove("active");
    });

    // Pagination Listeners
    btnPrev.addEventListener("click", () => {
        if (currentPage > 1) {
            currentPage--;
            render();
        }
    });

    btnNext.addEventListener("click", () => {
        const totalRows = RECEIPTS.length;
        const totalPages = Math.max(1, Math.ceil(totalRows / PAGE_SIZE));
        if (currentPage < totalPages) {
            currentPage++;
            render();
        }
    });

    // Initial renders
    renderDetailsCard();
    renderSummary();
    render();
});

// Render general metadata
function renderDetailsCard() {
    detailsTable.innerHTML = `
        <tr>
            <td class="info-label">Hotel Name</td>
            <td class="info-value" style="font-weight:600;">${selectedRow.hotel}</td>
        </tr>
        <tr>
            <td class="info-label">Total Amount</td>
            <td class="info-value" style="font-weight:600; color:var(--primary);">${selectedRow.amount}</td>
        </tr>
        <tr>
            <td class="info-label">Status</td>
            <td class="info-value">
                <span class="status-badge ${selectedRow.status.toLowerCase()}">${selectedRow.status}</span>
            </td>
        </tr>
        <tr>
            <td class="info-label">Generated Date</td>
            <td class="info-value">May 10, 2026</td>
        </tr>
        <tr>
            <td class="info-label">Due Since</td>
            <td class="info-value">${selectedRow.dueSince}</td>
        </tr>
    `;
}

// Render sub-bookings table
function render() {
    const totalRows = RECEIPTS.length;
    const totalPages = Math.max(1, Math.ceil(totalRows / PAGE_SIZE));
    
    // Bounds check
    if (currentPage > totalPages) {
        currentPage = totalPages;
    }
    
    const startIdx = (currentPage - 1) * PAGE_SIZE;
    const paged = RECEIPTS.slice(startIdx, startIdx + PAGE_SIZE);

    tableBody.innerHTML = "";
    paged.forEach(b => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td style="font-weight: 500;">${b.id}</td>
            <td>${b.customer}</td>
            <td>${b.total}</td>
            <td style="color: var(--text-muted);">${b.checkIn}</td>
            <td style="color: var(--text-muted);">${b.checkOut}</td>
            <td>
                <span class="status-badge paid" style="font-size: 0.75rem; padding: 2px 8px;">${b.paymentStatus}</span>
            </td>
            <td>
                <span class="status-badge paid" style="font-size: 0.75rem; padding: 2px 8px;">${b.bookingStatus}</span>
            </td>
            <td class="text-right">
                <button class="btn-action btn-outline" onclick="openReceiptModal('${b.id}')">
                    <i class="fa-regular fa-eye"></i> View
                </button>
            </td>
        `;
        tableBody.appendChild(tr);
    });

    // Render Pagination Info
    const startNum = totalRows === 0 ? 0 : startIdx + 1;
    const endNum = Math.min(startIdx + PAGE_SIZE, totalRows);
    paginationInfo.innerHTML = `Showing <strong>${startNum}-${endNum}</strong> of <strong>${totalRows}</strong> results`;

    // Toggle pagination disabled status
    btnPrev.disabled = currentPage === 1;
    btnNext.disabled = currentPage === totalPages;
    btnPrev.style.opacity = currentPage === 1 ? "0.5" : "1";
    btnNext.style.opacity = currentPage === totalPages ? "0.5" : "1";
}

// Render totals card summary
function renderSummary() {
    const summaryTable = document.getElementById("summary-table");
    
    const totalBookings = RECEIPTS.length;
    const totalGross = RECEIPTS.reduce((s, r) => s + parseFloat(r.total.replace("$", "")), 0);
    const totalFees = RECEIPTS.reduce((s, r) => s + parseFloat(r.fee.replace("$", "")), 0);
    const totalNet = totalGross - totalFees;

    summaryTable.innerHTML = `
        <tr>
            <td style="color: var(--text-muted);">Total Bookings</td>
            <td style="font-weight:600; text-align:right;">${totalBookings}</td>
        </tr>
        <tr>
            <td style="color: var(--text-muted);">Total Revenue (Gross)</td>
            <td style="font-weight:600; text-align:right;">${totalGross.toLocaleString()}$</td>
        </tr>
        <tr>
            <td style="color: var(--text-muted);">Platform Fees (10%)</td>
            <td style="font-weight:600; text-align:right; color:#ef4444;">${totalFees.toLocaleString()}$</td>
        </tr>
        <tr style="background: rgba(255, 255, 255, 0.03); font-weight: 700; border-top: 1.5px solid var(--border);">
            <td style="color: var(--text-main);">Final Payout Amount (Net)</td>
            <td style="text-align:right; color: #10b981; font-size:1.05rem;">${totalNet.toLocaleString()}$</td>
        </tr>
    `;
}

// Open booking receipt details modal
function openReceiptModal(id) {
    const b = RECEIPTS.find(r => r.id === id);
    if (!b) return;

    receiptModalBody.innerHTML = `
        <div class="overflow-hidden rounded-xl border border-border" style="margin-bottom: 20px;">
            <table class="w-full text-sm custom-table">
                <tbody>
                    <tr class="bg-muted/30">
                        <td class="info-label" style="padding: 10px 16px;">Booking ID</td>
                        <td class="info-value" style="padding: 10px 16px; font-weight:600;">${b.id}</td>
                    </tr>
                    <tr>
                        <td class="info-label" style="padding: 10px 16px;">Customer Name</td>
                        <td class="info-value" style="padding: 10px 16px;">${b.customer}</td>
                    </tr>
                    <tr class="bg-muted/30">
                        <td class="info-label" style="padding: 10px 16px;">Hotel Name</td>
                        <td class="info-value" style="padding: 10px 16px;">${selectedRow.hotel}</td>
                    </tr>
                    <tr>
                        <td class="info-label" style="padding: 10px 16px;">Booking Date</td>
                        <td class="info-value" style="padding: 10px 16px; color: var(--text-muted);">${b.booked}</td>
                    </tr>
                    <tr class="bg-muted/30">
                        <td class="info-label" style="padding: 10px 16px;">Check-In Date</td>
                        <td class="info-value" style="padding: 10px 16px; color: var(--text-muted);">${b.checkIn}</td>
                    </tr>
                    <tr>
                        <td class="info-label" style="padding: 10px 16px;">Check-Out Date</td>
                        <td class="info-value" style="padding: 10px 16px; color: var(--text-muted);">${b.checkOut}</td>
                    </tr>
                </tbody>
            </table>
        </div>

        <h3 class="receipt-section-title">Room Specifications</h3>
        <div class="table-wrapper" style="margin-bottom: 20px;">
            <table class="custom-table">
                <thead>
                    <tr>
                        <th>Room Type</th>
                        <th>Bed Specifications</th>
                        <th>Quantity</th>
                        <th>Nights</th>
                        <th>Price/Night</th>
                        <th class="text-right">Total Price</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td style="font-weight:500;">${b.roomType}</td>
                        <td>${b.bedType}</td>
                        <td>${b.rooms}</td>
                        <td>${b.nights}</td>
                        <td>${b.pricePerNight}</td>
                        <td class="text-right" style="font-weight:600;">${b.totalRoomPrice}</td>
                    </tr>
                </tbody>
            </table>
        </div>

        <h3 class="receipt-section-title">Billing & Transaction Details</h3>
        <div class="overflow-hidden rounded-xl border border-border">
            <table class="w-full text-sm custom-table">
                <tbody>
                    <tr class="bg-muted/30">
                        <td class="info-label" style="padding: 10px 16px;">Payment Method</td>
                        <td class="info-value" style="padding: 10px 16px;">${b.paymentMethod} Card</td>
                    </tr>
                    <tr>
                        <td class="info-label" style="padding: 10px 16px;">Payment Settlement Date</td>
                        <td class="info-value" style="padding: 10px 16px; color: var(--text-muted);">${b.paymentDate}</td>
                    </tr>
                    <tr class="bg-muted/30">
                        <td class="info-label" style="padding: 10px 16px; font-weight:600;">Paid Amount (Gross)</td>
                        <td class="info-value" style="padding: 10px 16px; font-weight:600; color: var(--primary);">${b.total}</td>
                    </tr>
                    <tr>
                        <td class="info-label" style="padding: 10px 16px; font-weight:600;">Platform Fee (10%)</td>
                        <td class="info-value" style="padding: 10px 16px; font-weight:600; color: #ef4444;">${b.fee}</td>
                    </tr>
                    <tr class="bg-muted/30">
                        <td class="info-label" style="padding: 10px 16px; font-weight:600;">Net payout for Hotel</td>
                        <td class="info-value" style="padding: 10px 16px; font-weight:600; color: #10b981;">${b.net}</td>
                    </tr>
                </tbody>
            </table>
        </div>
    `;
    receiptModal.classList.add("active");
}

// Confirm payout execution
function executeConfirmPayout() {
    selectedRow.status = "Paid";
    selectedRow.dueSince = "Paid today";
    
    // Hide confirm action
    confirmBtn.style.display = "none";
    
    // Show toast and reload metadata
    showToast(`Payout to ${selectedRow.hotel} confirmed successfully.`);
    renderDetailsCard();
    
    confirmModal.classList.remove("active");
}

function showToast(message) {
    const toastContainer = document.getElementById("toast-container");
    const toast = document.createElement("div");
    toast.className = "toast-message";
    toast.innerHTML = `<i class="fa-solid fa-circle-check"></i> ${message}`;
    toastContainer.appendChild(toast);
    
    // Trigger slide-in
    setTimeout(() => toast.classList.add("active"), 50);
    
    // Slide-out and redirect to dashboard
    setTimeout(() => {
        toast.classList.remove("active");
        setTimeout(() => {
            toast.remove();
            window.location.href = "admin-payout.html";
        }, 300);
    }, 2500);
}

// Export Receipt handler
function exportReceipt() {
    showToast("Exporting payout summary and receipts sheet...");
}
