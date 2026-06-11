// Admin Commission Settings Logic

const API_BASE = '/api/admin/commissions';

let currentServiceType = 'Hotel';
let timerInterval;

// DOM Elements
const tabsContainer = document.getElementById('tabs-container');
const tabButtons = tabsContainer.querySelectorAll('.tab-btn');
const serviceTitle = document.getElementById('current-service-title');

const activePercentage = document.getElementById('active-percentage');
const activeEffective = document.getElementById('active-effective');

const pendingBox = document.getElementById('pending-box');
const pendingPercentage = document.getElementById('pending-percentage');
const pendingEffective = document.getElementById('pending-effective');

// Timer elements
const timerDays = document.getElementById('timer-days');
const timerHours = document.getElementById('timer-hours');
const timerMinutes = document.getElementById('timer-minutes');
const timerSeconds = document.getElementById('timer-seconds');

// Form
const updateForm = document.getElementById('update-form');
const inputPercentage = document.getElementById('new-percentage');
const inputNotes = document.getElementById('update-notes');
const btnSave = document.getElementById('btn-save-commission');
const formError = document.getElementById('form-error');

// History
const historyBody = document.getElementById('history-body');

document.addEventListener('DOMContentLoaded', () => {
    // Check Auth Token
    if (!localStorage.getItem('token')) {
        window.location.href = '../auth/login.html';
        return;
    }

    // Bind Tabs
    tabButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            tabButtons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            currentServiceType = btn.getAttribute('data-tab');
            loadServiceData(currentServiceType);
        });
    });

    // Bind Save Button
    btnSave.addEventListener('click', saveCommission);

    // Initial Load
    loadServiceData(currentServiceType);
});

async function loadServiceData(serviceType) {
    serviceTitle.textContent = `${serviceType} Commission Details`;
    formError.textContent = '';
    inputPercentage.value = '';
    inputNotes.value = '';
    
    clearInterval(timerInterval);
    pendingBox.style.display = 'none';
    
    try {
        const token = localStorage.getItem('token');
        const headers = { 'Authorization': `Bearer ${token}` };

        // Fetch Current/Pending
        const res = await fetch(`${API_BASE}/${serviceType}`, { headers });
        if (!res.ok) throw new Error('Failed to load commission data');
        
        const data = await res.json();
        
        // Render Active
        if (data.activeCommission) {
            activePercentage.textContent = `${data.activeCommission.percentage}%`;
            activeEffective.textContent = `Effective from: ${new Date(data.activeCommission.effectiveFrom).toLocaleDateString()}`;
        } else {
            activePercentage.textContent = 'Not Set';
            activeEffective.textContent = 'Effective from: N/A';
        }

        // Render Pending
        if (data.pendingCommission) {
            pendingBox.style.display = 'block';
            pendingPercentage.textContent = `${data.pendingCommission.percentage}%`;
            pendingEffective.textContent = `Will be active at: ${new Date(data.pendingCommission.effectiveFrom).toLocaleDateString()}`;
            
            // Disable form if pending exists
            updateForm.style.opacity = '0.5';
            inputPercentage.disabled = true;
            inputNotes.disabled = true;
            btnSave.disabled = true;
            
            startTimer(data.remainingDays, data.remainingHours, data.remainingMinutes, data.remainingSeconds);
        } else {
            updateForm.style.opacity = '1';
            inputPercentage.disabled = false;
            inputNotes.disabled = false;
            btnSave.disabled = false;
        }

        // Fetch History
        const historyRes = await fetch(`${API_BASE}/${serviceType}/history`, { headers });
        if (historyRes.ok) {
            const historyData = await historyRes.json();
            renderHistory(historyData);
        }

    } catch (err) {
        console.error(err);
        showToast('Error loading data', true);
    }
}

async function saveCommission() {
    const pctStr = inputPercentage.value.trim();
    if (!pctStr) {
        formError.textContent = 'Please enter a percentage.';
        return;
    }
    
    const pct = parseFloat(pctStr);
    if (isNaN(pct) || pct < 0 || pct > 100) {
        formError.textContent = 'Percentage must be between 0 and 100.';
        return;
    }

    formError.textContent = '';
    btnSave.disabled = true;
    btnSave.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Saving...';

    try {
        const token = localStorage.getItem('token');
        const reqBody = {
            percentage: pct,
            notes: inputNotes.value.trim()
        };

        const res = await fetch(`${API_BASE}/${currentServiceType}`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(reqBody)
        });

        if (!res.ok) {
            const errText = await res.text();
            throw new Error(errText || 'Failed to save commission');
        }

        showToast('Commission update scheduled successfully!');
        
        // Reload UI
        loadServiceData(currentServiceType);

    } catch (err) {
        console.error(err);
        formError.textContent = err.message;
    } finally {
        btnSave.innerHTML = '<i class="fa-solid fa-floppy-disk"></i> Save Update';
    }
}

function renderHistory(historyArray) {
    historyBody.innerHTML = '';
    
    if (!historyArray || historyArray.length === 0) {
        historyBody.innerHTML = `<tr><td colspan="5" style="text-align: center;">No history found.</td></tr>`;
        return;
    }

    historyArray.forEach(item => {
        const tr = document.createElement('tr');
        
        let statusClass = 'old';
        if (item.status === 'Active') statusClass = 'active';
        if (item.status === 'Scheduled') statusClass = 'scheduled';

        let actionsHtml = '-';
        if (item.status === 'Scheduled') {
            actionsHtml = `<button class="btn-delete" onclick="deleteCommission(${item.id})" title="Delete Scheduled Update"><i class="fa-solid fa-trash"></i></button>`;
        }

        tr.innerHTML = `
            <td style="font-weight: 600;">${item.percentage}%</td>
            <td>${new Date(item.effectiveFrom).toLocaleDateString()}</td>
            <td>${item.createdByAdminName || 'System'}</td>
            <td style="color: var(--text-muted);">${item.notes || '-'}</td>
            <td><span class="status-badge ${statusClass}">${item.status}</span></td>
            <td>${actionsHtml}</td>
        `;
        historyBody.appendChild(tr);
    });
}

async function deleteCommission(id) {
    if (!confirm('Are you sure you want to delete this scheduled commission update?')) {
        return;
    }

    try {
        const token = localStorage.getItem('token');
        const res = await fetch(`${API_BASE}/${id}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (!res.ok) {
            const errText = await res.text();
            throw new Error(errText || 'Failed to delete commission');
        }

        showToast('Scheduled commission deleted successfully!');
        
        // Reload UI
        loadServiceData(currentServiceType);

    } catch (err) {
        console.error(err);
        showToast(err.message, true);
    }
}

function startTimer(d, h, m, s) {
    let totalSeconds = (d * 86400) + (h * 3600) + (m * 60) + s;

    const updateUI = () => {
        if (totalSeconds <= 0) {
            clearInterval(timerInterval);
            timerDays.textContent = '00';
            timerHours.textContent = '00';
            timerMinutes.textContent = '00';
            timerSeconds.textContent = '00';
            // Auto reload when timer finishes
            loadServiceData(currentServiceType);
            return;
        }

        let days = Math.floor(totalSeconds / 86400);
        let rem = totalSeconds % 86400;
        let hours = Math.floor(rem / 3600);
        rem = rem % 3600;
        let minutes = Math.floor(rem / 60);
        let seconds = rem % 60;

        timerDays.textContent = String(days).padStart(2, '0');
        timerHours.textContent = String(hours).padStart(2, '0');
        timerMinutes.textContent = String(minutes).padStart(2, '0');
        timerSeconds.textContent = String(seconds).padStart(2, '0');
    };

    updateUI(); // run once immediately
    
    timerInterval = setInterval(() => {
        totalSeconds--;
        updateUI();
    }, 1000);
}

function showToast(message, isError = false) {
    const container = document.getElementById('toast-container');
    const toast = document.createElement('div');
    toast.className = `toast-message ${isError ? 'error' : ''}`;
    
    const icon = isError ? '<i class="fa-solid fa-circle-exclamation"></i>' : '<i class="fa-solid fa-circle-check"></i>';
    toast.innerHTML = `${icon} ${message}`;
    
    container.appendChild(toast);
    
    setTimeout(() => toast.classList.add('active'), 50);
    
    setTimeout(() => {
        toast.classList.remove('active');
        setTimeout(() => toast.remove(), 300);
    }, 3500);
}
