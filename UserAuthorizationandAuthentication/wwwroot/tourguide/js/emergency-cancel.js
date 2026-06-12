document.addEventListener('DOMContentLoaded', () => {
    if (!Auth.isLoggedIn()) {
        Auth.logout();
        return;
    }

    const form = document.getElementById('emergency-cancel-form');
    if (form) {
        form.addEventListener('submit', handleEmergencyCancelSubmit);
    }
});

async function handleEmergencyCancelSubmit(e) {
    e.preventDefault();

    const tourIdInput = document.getElementById('tour-id');
    const reasonInput = document.getElementById('cancel-reason');
    const submitBtn = document.getElementById('submit-btn');
    const btnText = document.getElementById('btn-text');
    const btnSpinner = submitBtn.querySelector('.btn-spinner');

    const tourId = parseInt(tourIdInput.value);
    const reason = reasonInput.value.trim();

    if (isNaN(tourId) || tourId <= 0 || !reason) {
        showToast('Please provide a valid Tour ID and Reason.', 'error');
        return;
    }

    // Set loading state
    submitBtn.disabled = true;
    btnText.style.display = 'none';
    btnSpinner.style.display = 'block';

    try {
        const response = await API.post('/tourguide/urgent-requests/submit', {
            tourId: tourId,
            reason: reason
        });

        if (response.success) {
            showToast('Emergency cancellation submitted successfully. Affected user bookings are now pending decision.', 'success');
            e.target.reset();
        } else {
            showToast(`Submission failed: ${response.message || 'Unknown error'}`, 'error');
        }
    } catch (err) {
        showToast(`Submission failed: ${err.message}`, 'error');
    } finally {
        // Reset button state
        submitBtn.disabled = false;
        btnText.style.display = 'block';
        btnSpinner.style.display = 'none';
    }
}

function showToast(message, type = 'success') {
    const toast = document.getElementById('status-toast');
    if (!toast) return;

    toast.textContent = message;
    toast.className = `toast toast-${type}`;
    toast.style.display = 'block';

    setTimeout(() => {
        toast.style.display = 'none';
    }, 5000);
}
