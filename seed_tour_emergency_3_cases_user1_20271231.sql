-- Seed Script for TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231
-- This will create 3 cancelled tours and 3 alternative tours for user1@gmail.com

BEGIN TRANSACTION;

DECLARE @UserId BIGINT;
DECLARE @GuideUserId BIGINT;
DECLARE @GuideId BIGINT;
DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @TourDate DATETIME2 = '2027-12-31 09:00:00';
DECLARE @Marker NVARCHAR(100) = 'TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231';

-- 1. Find User and Guide
SELECT TOP 1 @UserId = Id FROM Users WHERE Email = 'user1@gmail.com';
SELECT TOP 1 @GuideUserId = Id FROM Users WHERE Email = 'testguide_emergency@example.com';
IF @GuideUserId IS NULL
    SELECT TOP 1 @GuideUserId = Id FROM Users WHERE Email = 'testguide@example.com';

SELECT TOP 1 @GuideId = Id FROM tourguide_TourGuides WHERE UserId = @GuideUserId;

IF @UserId IS NULL OR @GuideId IS NULL
BEGIN
    PRINT 'Required users not found. Cannot seed data.';
    ROLLBACK TRANSACTION;
    RETURN;
END

-- 2. Ensure User Wallet has enough balance for testing difference payment
UPDATE Users SET WalletBalance = WalletBalance + 500.00 WHERE Id = @UserId;

-- Create Transaction for Wallet
INSERT INTO airline_WalletTransactions (UserId, Amount, Type, ReferenceId, CreatedAt, Description)
VALUES (@UserId, 500.00, 'Deposit', 'Seed', @Now, @Marker + ' Wallet Topup');

-- =========================================================================
-- CASE 1: ORIGINAL_REFUND_WALLET
-- =========================================================================
DECLARE @Tour1Id BIGINT;
INSERT INTO tourguide_Tours (TourGuideId, TourTitle, TourDescription, City, BasePriceUsd, Currency, Active, AvailableDateTime, GroupSizeMax, TransportIncluded, MealsIncluded, IsAccessible, Customizable, CreatedAt, CancellationPolicy)
VALUES (@GuideId, @Marker + '_ORIGINAL_REFUND_WALLET', @Marker, 'Cairo', 100.00, 'USD', 0, @TourDate, 10, 0, 0, 0, 0, @Now, 1);
SET @Tour1Id = SCOPE_IDENTITY();

DECLARE @Booking1Id BIGINT;
INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, ParticipantsCount, TotalPrice, Currency, Status, PaymentStatus, BookingDate, TourDate, TourTime, SpecialRequests, CreatedAt)
VALUES (@UserId, @Tour1Id, @GuideId, 1, 100.00, 'USD', 4, 1, @Now, CAST(@TourDate AS DATE), CAST(@TourDate AS TIME), @Marker + '_ORIGINAL_REFUND_WALLET', @Now);
SET @Booking1Id = SCOPE_IDENTITY();

-- CheckoutSession
DECLARE @CS1Id BIGINT;
INSERT INTO CheckoutSessions (UserId, CheckoutType, Status, StripeCheckoutSessionId, CreatedAt, ExpiresAt, TotalAmount, Currency)
VALUES (@UserId, 'HotelTour', 'Paid', @Marker + '_CS1', @Now, DATEADD(hour, 1, @Now), 100.00, 'USD');
SET @CS1Id = SCOPE_IDENTITY();

-- Wallet Payment Transaction
DECLARE @Pay1Id BIGINT;
INSERT INTO PaymentTransactions (CheckoutSessionId, UserId, Amount, Currency, Status, Provider, ProviderTransactionId, PaymentMethod, CreatedAt)
VALUES (@CS1Id, @UserId, 100.00, 'USD', 'Completed', 'Wallet', @Marker + '_WALLET_' + CAST(@Booking1Id AS NVARCHAR(20)), 'Wallet', @Now);
SET @Pay1Id = SCOPE_IDENTITY();

INSERT INTO PaymentTransactionItems (PaymentTransactionId, BookingType, BookingId, Amount, Currency, Status, CreatedAt)
VALUES (@Pay1Id, 'Tour', @Booking1Id, 100.00, 'USD', 'Completed', @Now);

-- =========================================================================
-- CASE 2: ORIGINAL_REFUND_STRIPE
-- =========================================================================
DECLARE @Tour2Id BIGINT;
INSERT INTO tourguide_Tours (TourGuideId, TourTitle, TourDescription, City, BasePriceUsd, Currency, Active, AvailableDateTime, GroupSizeMax, TransportIncluded, MealsIncluded, IsAccessible, Customizable, CreatedAt, CancellationPolicy)
VALUES (@GuideId, @Marker + '_ORIGINAL_REFUND_STRIPE', @Marker, 'Cairo', 100.00, 'USD', 0, @TourDate, 10, 0, 0, 0, 0, @Now, 1);
SET @Tour2Id = SCOPE_IDENTITY();

DECLARE @Booking2Id BIGINT;
INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, ParticipantsCount, TotalPrice, Currency, Status, PaymentStatus, BookingDate, TourDate, TourTime, SpecialRequests, CreatedAt)
VALUES (@UserId, @Tour2Id, @GuideId, 1, 100.00, 'USD', 4, 1, @Now, CAST(@TourDate AS DATE), CAST(@TourDate AS TIME), @Marker + '_ORIGINAL_REFUND_STRIPE', @Now);
SET @Booking2Id = SCOPE_IDENTITY();

-- CheckoutSession
DECLARE @CS2Id BIGINT;
INSERT INTO CheckoutSessions (UserId, CheckoutType, Status, StripeCheckoutSessionId, CreatedAt, ExpiresAt, TotalAmount, Currency)
VALUES (@UserId, 'HotelTour', 'Paid', @Marker + '_CS2', @Now, DATEADD(hour, 1, @Now), 100.00, 'USD');
SET @CS2Id = SCOPE_IDENTITY();

-- Stripe Payment Transaction
DECLARE @Pay2Id BIGINT;
INSERT INTO PaymentTransactions (CheckoutSessionId, UserId, Amount, Currency, Status, Provider, ProviderTransactionId, PaymentMethod, CreatedAt)
VALUES (@CS2Id, @UserId, 100.00, 'USD', 'Completed', 'Stripe', @Marker + '_pi_test_2', 'Card', @Now);
SET @Pay2Id = SCOPE_IDENTITY();

INSERT INTO PaymentTransactionItems (PaymentTransactionId, BookingType, BookingId, Amount, Currency, Status, CreatedAt)
VALUES (@Pay2Id, 'Tour', @Booking2Id, 100.00, 'USD', 'Completed', @Now);

-- =========================================================================
-- CASE 3: ORIGINAL_ALTERNATIVE
-- =========================================================================
DECLARE @Tour3Id BIGINT;
INSERT INTO tourguide_Tours (TourGuideId, TourTitle, TourDescription, City, BasePriceUsd, Currency, Active, AvailableDateTime, GroupSizeMax, TransportIncluded, MealsIncluded, IsAccessible, Customizable, CreatedAt, CancellationPolicy)
VALUES (@GuideId, @Marker + '_ORIGINAL_ALTERNATIVE', @Marker, 'Cairo', 100.00, 'USD', 0, @TourDate, 10, 0, 0, 0, 0, @Now, 1);
SET @Tour3Id = SCOPE_IDENTITY();

DECLARE @Booking3Id BIGINT;
INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, ParticipantsCount, TotalPrice, Currency, Status, PaymentStatus, BookingDate, TourDate, TourTime, SpecialRequests, CreatedAt)
VALUES (@UserId, @Tour3Id, @GuideId, 1, 100.00, 'USD', 4, 1, @Now, CAST(@TourDate AS DATE), CAST(@TourDate AS TIME), @Marker + '_ORIGINAL_ALTERNATIVE', @Now);
SET @Booking3Id = SCOPE_IDENTITY();

-- CheckoutSession
DECLARE @CS3Id BIGINT;
INSERT INTO CheckoutSessions (UserId, CheckoutType, Status, StripeCheckoutSessionId, CreatedAt, ExpiresAt, TotalAmount, Currency)
VALUES (@UserId, 'HotelTour', 'Paid', @Marker + '_CS3', @Now, DATEADD(hour, 1, @Now), 100.00, 'USD');
SET @CS3Id = SCOPE_IDENTITY();

-- Stripe Payment Transaction for Alt
DECLARE @Pay3Id BIGINT;
INSERT INTO PaymentTransactions (CheckoutSessionId, UserId, Amount, Currency, Status, Provider, ProviderTransactionId, PaymentMethod, CreatedAt)
VALUES (@CS3Id, @UserId, 100.00, 'USD', 'Completed', 'Stripe', @Marker + '_pi_test_3', 'Card', @Now);
SET @Pay3Id = SCOPE_IDENTITY();

INSERT INTO PaymentTransactionItems (PaymentTransactionId, BookingType, BookingId, Amount, Currency, Status, CreatedAt)
VALUES (@Pay3Id, 'Tour', @Booking3Id, 100.00, 'USD', 'Completed', @Now);

-- =========================================================================
-- Create Urgent Requests (Guide Cancellation) for the 3 original tours
-- =========================================================================
INSERT INTO tourguide_UrgentRequests (TourGuideId, TourId, Reason, Status, CreatedAt)
VALUES 
(@GuideId, @Tour1Id, @Marker + ' Emergency Cancel 1', 0, @Now),
(@GuideId, @Tour2Id, @Marker + ' Emergency Cancel 2', 0, @Now),
(@GuideId, @Tour3Id, @Marker + ' Emergency Cancel 3', 0, @Now);

-- =========================================================================
-- Create 3 Alternative Tours (Active, Same Date/Time, Same City)
-- 1. Cheaper, 2. Equal, 3. More Expensive
-- =========================================================================
INSERT INTO tourguide_Tours (TourGuideId, TourTitle, TourDescription, City, BasePriceUsd, Currency, Active, AvailableDateTime, GroupSizeMax, TransportIncluded, MealsIncluded, IsAccessible, Customizable, CreatedAt, CancellationPolicy)
VALUES 
(@GuideId, @Marker + '_ALT_CHEAPER', @Marker, 'Cairo', 90.00, 'USD', 1, @TourDate, 10, 0, 0, 0, 0, @Now, 1),
(@GuideId, @Marker + '_ALT_EQUAL', @Marker, 'Cairo', 105.26, 'USD', 1, @TourDate, 10, 0, 0, 0, 0, @Now, 1),
(@GuideId, @Marker + '_ALT_MORE', @Marker, 'Cairo', 120.00, 'USD', 1, @TourDate, 10, 0, 0, 0, 0, @Now, 1);

COMMIT;
PRINT 'Seed successful for TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231';
