-- Fix Seed Data Statuses
UPDATE tourguide_TourBookings
SET Status = 'PendingUserDecision', PaymentStatus = 'Completed'
WHERE SpecialRequests LIKE '%TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231%';
