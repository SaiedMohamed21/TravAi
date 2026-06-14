-- Cleanup Script for TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231
-- This will delete all seed data associated with this marker

BEGIN TRANSACTION;

-- 1. Delete Urgent Requests
DELETE FROM tourguide_UrgentRequests WHERE Reason LIKE '%TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231%';

-- 2. Delete Tour Booking Resolutions
DELETE FROM tourguide_TourBookingResolutions 
WHERE OriginalBookingId IN (
    SELECT Id FROM tourguide_TourBookings WHERE SpecialRequests LIKE '%TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231%'
);

-- 3. Delete Payment Transaction Items and Transactions
DELETE FROM PaymentTransactionItems 
WHERE BookingId IN (
    SELECT Id FROM tourguide_TourBookings WHERE SpecialRequests LIKE '%TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231%'
);

DELETE FROM PaymentTransactions 
WHERE ProviderTransactionId LIKE '%TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231%';

DELETE FROM CheckoutSessions
WHERE StripeCheckoutSessionId LIKE '%TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231%';

-- 4. Delete Wallet Transactions
DELETE FROM airline_WalletTransactions 
WHERE Description LIKE '%TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231%';

-- 5. Delete Tour Bookings
DELETE FROM tourguide_TourBookings WHERE SpecialRequests LIKE '%TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231%';

-- 6. Delete Coupons
DELETE FROM tourguide_UserTourCompensationCoupons WHERE CouponCode LIKE '%TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231%';

-- 7. Delete Tours
DELETE FROM tourguide_Tours WHERE TourTitle LIKE '%TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231%';

COMMIT;
