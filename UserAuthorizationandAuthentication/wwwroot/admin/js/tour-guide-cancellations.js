document.addEventListener('DOMContentLoaded', () => {
    // Check auth
    if (!Auth.isLoggedIn()) {
        Auth.logout();
        return;
    }

    // Load pending cancellations for review
    loadPendingReviews();
});

async function loadPendingReviews() {
    showState('loader');

    const response = await API.get('/admin/tour-guide-cancellations/pending-review');

    if (!response.success) {
        showError(response.message || 'Failed to retrieve pending cancellation requests.', 'API Fetch Error');
        return;
    }

    const reviews = response.data || [];
    renderPendingReviews(reviews);
}

function renderPendingReviews(reviews) {
    const grid = document.getElementById('review-grid');
    const emptyState = document.getElementById('empty-state');

    if (reviews.length === 0) {
        showState('empty');
        return;
    }

    if (grid) {
        grid.innerHTML = '';
        reviews.forEach(item => {
            const card = createReviewCard(item);
            grid.appendChild(card);
        });
    }

    showState('content');
}

function createReviewCard(item) {
    const card = document.createElement('div');
    card.className = 'review-card';

    // Normalize properties (support camelCase and PascalCase)
    const id = item.id || item.Id;
    const guideName = item.tourGuideName || item.TourGuideName || 'Unknown Guide';
    const tourName = item.tourName || item.TourName || 'Unknown Tour';
    const destination = item.destination || item.Destination || 'Unknown Destination';
    const reason = item.reason || item.Reason || 'No reason provided';
    const dateStr = item.createdAt || item.CreatedAt;
    const affectedCount = item.affectedBookingsCount !== undefined ? item.affectedBookingsCount : (item.AffectedBookingsCount || 0);
    const status = item.status || item.Status || 'Pending';

    const formattedDate = dateStr ? new Date(dateStr).toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    }) : 'TBD';

    card.innerHTML = `
        <div class="card-header-row">
            <div class="guide-details">
                <h2>${tourName}</h2>
                <div class="guide-name">Guide: <span>${guideName}</span></div>
            </div>
            <div class="affected-badge">
                <i class="fas fa-users"></i> ${affectedCount} affected
            </div>
        </div>

        <div class="info-box">
            <div class="info-item">
                <i class="fas fa-map-marker-alt"></i>
                <span>Destination: ${destination}</span>
            </div>
            <div class="info-item">
                <i class="fas fa-calendar-day"></i>
                <span>Submitted: ${formattedDate}</span>
            </div>
            <div class="info-item">
                <i class="fas fa-tag"></i>
                <span>Status: ${status}</span>
            </div>
        </div>

        <!-- Guide Reason -->
        <div class="reason-box">
            <h4>Emergency Reason Given</h4>
            <p>"${reason}"</p>
        </div>

        <!-- Admin Notes -->
        <div class="notes-group">
            <label for="notes-${id}">Admin Decision Notes</label>
            <textarea id="notes-${id}" placeholder="Specify reasoning for accepting/rejecting this cancellation..." maxlength="500"></textarea>
        </div>

        <!-- Action Buttons -->
        <div class="review-actions">
            <button onclick="submitReview(${id}, true)" class="btn-accept">
                <i class="fas fa-check"></i> Accept Reason
            </button>
            <button onclick="submitReview(${id}, false)" class="btn-reject">
                <i class="fas fa-times"></i> Reject Reason
            </button>
        </div>
    `;

    return card;
}

async function submitReview(id, isReasonAccepted) {
    const notesInput = document.getElementById(`notes-${id}`);
    const notes = notesInput ? notesInput.value.trim() : '';

    if (!notes) {
        alert('Please specify administrative review notes before completing the action.');
        return;
    }

    const actionText = isReasonAccepted ? 'accept' : 'reject';
    if (!confirm(`Are you sure you want to ${actionText} the guide's cancellation reason?`)) {
        return;
    }

    showState('loader');

    // Endpoint: POST /api/admin/tour-guide-cancellations/{id}/review
    const response = await API.post(`/admin/tour-guide-cancellations/${id}/review`, {
        isReasonAccepted: isReasonAccepted,
        adminNotes: notes
    });

    if (response.success) {
        alert(`Successfully ${isReasonAccepted ? 'accepted' : 'rejected'} the cancellation request.`);
        loadPendingReviews();
    } else {
        alert(`Failed to submit review: ${response.message || 'Error occurred'}`);
        loadPendingReviews();
    }
}

function showState(state) {
    const loader = document.getElementById('loader');
    const error = document.getElementById('error-message');
    const empty = document.getElementById('empty-state');
    const grid = document.getElementById('review-grid');

    if (loader) loader.style.display = state === 'loader' ? 'flex' : 'none';
    if (error) error.style.display = state === 'error' ? 'block' : 'none';
    if (empty) empty.style.display = state === 'empty' ? 'block' : 'none';
    if (grid) grid.style.display = state === 'content' ? 'grid' : 'none';
}

function showError(text, title = 'Error') {
    const errorEl = document.getElementById('error-message');
    const errorTitle = document.getElementById('error-title');
    const errorText = document.getElementById('error-text');

    if (errorTitle) errorTitle.textContent = title;
    if (errorText) errorText.textContent = text;
    
    showState('error');
}
