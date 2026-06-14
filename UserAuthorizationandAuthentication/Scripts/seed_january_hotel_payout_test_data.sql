-- Scripts/seed_january_hotel_payout_test_data.sql

BEGIN TRANSACTION;

-- Marker check
IF EXISTS (SELECT 1 FROM hotel_Hotels WHERE HotelName LIKE '%PAYOUT_TEST_JANUARY_2026%')
BEGIN
    PRINT 'PAYOUT_TEST_JANUARY_2026 data already exists. Skipping insertion.';
    COMMIT TRANSACTION;
    RETURN;
END

DECLARE @UserId BIGINT;
-- Pick any admin user or just any user
SELECT TOP 1 @UserId = Id FROM Users;

IF @UserId IS NULL
BEGIN
    PRINT 'No user found in Users. Test data requires at least one user.';
    ROLLBACK TRANSACTION;
    RETURN;
END

-- Get Stripe Account Id
DECLARE @StripeAccountId NVARCHAR(255);
SELECT TOP 1 @StripeAccountId = StripeConnectedAccountId
FROM admin_ProviderStripePayoutAccounts
WHERE StripeConnectedAccountId IS NOT NULL
GROUP BY StripeConnectedAccountId
ORDER BY COUNT(*) DESC;

IF @StripeAccountId IS NULL
BEGIN
    SET @StripeAccountId = 'acct_test_fallback_january_2026';
END

-- Variables for newly inserted Hotels
DECLARE @HotelA_Id BIGINT;
DECLARE @HotelB_Id BIGINT;
DECLARE @HotelC_Id BIGINT;
DECLARE @HotelD_Id BIGINT;

-- Insert Hotel A
INSERT INTO hotel_Hotels (UserId, HotelName, VerificationStatus, Active, Verified, PriceUsd, NumReviews, AvgReviewScore, CreatedAt, UpdatedAt)
VALUES (@UserId, 'PAYOUT_TEST_JANUARY_2026_HOTEL_A_NO_REFUND', 1, 1, 1, 0, 0, 0, GETUTCDATE(), GETUTCDATE());
SELECT @HotelA_Id = Id FROM hotel_Hotels WHERE HotelName = 'PAYOUT_TEST_JANUARY_2026_HOTEL_A_NO_REFUND';

-- Insert Hotel B
INSERT INTO hotel_Hotels (UserId, HotelName, VerificationStatus, Active, Verified, PriceUsd, NumReviews, AvgReviewScore, CreatedAt, UpdatedAt)
VALUES (@UserId, 'PAYOUT_TEST_JANUARY_2026_HOTEL_B_WITH_REFUNDS', 1, 1, 1, 0, 0, 0, GETUTCDATE(), GETUTCDATE());
SELECT @HotelB_Id = Id FROM hotel_Hotels WHERE HotelName = 'PAYOUT_TEST_JANUARY_2026_HOTEL_B_WITH_REFUNDS';

-- Insert Hotel C
INSERT INTO hotel_Hotels (UserId, HotelName, VerificationStatus, Active, Verified, PriceUsd, NumReviews, AvgReviewScore, CreatedAt, UpdatedAt)
VALUES (@UserId, 'PAYOUT_TEST_JANUARY_2026_HOTEL_C_ONE_FINE', 1, 1, 1, 0, 0, 0, GETUTCDATE(), GETUTCDATE());
SELECT @HotelC_Id = Id FROM hotel_Hotels WHERE HotelName = 'PAYOUT_TEST_JANUARY_2026_HOTEL_C_ONE_FINE';

-- Insert Hotel D
INSERT INTO hotel_Hotels (UserId, HotelName, VerificationStatus, Active, Verified, PriceUsd, NumReviews, AvgReviewScore, CreatedAt, UpdatedAt)
VALUES (@UserId, 'PAYOUT_TEST_JANUARY_2026_HOTEL_D_TWO_FINES', 1, 1, 1, 0, 0, 0, GETUTCDATE(), GETUTCDATE());
SELECT @HotelD_Id = Id FROM hotel_Hotels WHERE HotelName = 'PAYOUT_TEST_JANUARY_2026_HOTEL_D_TWO_FINES';

-- Stripe Accounts (ProviderType = 1 for Hotel)
INSERT INTO admin_ProviderStripePayoutAccounts (ProviderType, ProviderId, StripeConnectedAccountId, ProviderPayoutAccountNumber, Currency, IsActive, CreatedAt, UpdatedAt)
VALUES 
(1, @HotelA_Id, @StripeAccountId, 'dummy_num_jan', 'USD', 1, GETUTCDATE(), GETUTCDATE()),
(1, @HotelB_Id, @StripeAccountId, 'dummy_num_jan', 'USD', 1, GETUTCDATE(), GETUTCDATE()),
(1, @HotelC_Id, @StripeAccountId, 'dummy_num_jan', 'USD', 1, GETUTCDATE(), GETUTCDATE()),
(1, @HotelD_Id, @StripeAccountId, 'dummy_num_jan', 'USD', 1, GETUTCDATE(), GETUTCDATE());


-- HOTEL A BOOKINGS (Jan 5 - Jan 11)
DECLARE @i INT = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO hotel_HotelBookings (HotelId, UserId, CreatedAt, UpdatedAt, CheckInDate, CheckOutDate, TotalPrice, PaymentStatus, Status, RefundAmount, TotalRooms)
    VALUES (@HotelA_Id, @UserId, '2025-12-01', '2025-12-01', '2026-01-05', '2026-01-08', 100.00, 1, 1, 0, 1);
    SET @i = @i + 1;
END

-- HOTEL B BOOKINGS (Jan 12 - Jan 18)
INSERT INTO hotel_HotelBookings (HotelId, UserId, CreatedAt, UpdatedAt, CheckInDate, CheckOutDate, TotalPrice, PaymentStatus, Status, RefundAmount, TotalRooms)
VALUES (@HotelB_Id, @UserId, '2025-12-01', '2025-12-01', '2026-01-12', '2026-01-15', 100.00, 1, 1, 20.00, 1);
INSERT INTO hotel_HotelBookings (HotelId, UserId, CreatedAt, UpdatedAt, CheckInDate, CheckOutDate, TotalPrice, PaymentStatus, Status, RefundAmount, TotalRooms)
VALUES (@HotelB_Id, @UserId, '2025-12-01', '2025-12-01', '2026-01-12', '2026-01-15', 100.00, 1, 1, 50.00, 1);
INSERT INTO hotel_HotelBookings (HotelId, UserId, CreatedAt, UpdatedAt, CheckInDate, CheckOutDate, TotalPrice, PaymentStatus, Status, RefundAmount, TotalRooms)
VALUES (@HotelB_Id, @UserId, '2025-12-01', '2025-12-01', '2026-01-12', '2026-01-15', 100.00, 1, 1, 100.00, 1);
INSERT INTO hotel_HotelBookings (HotelId, UserId, CreatedAt, UpdatedAt, CheckInDate, CheckOutDate, TotalPrice, PaymentStatus, Status, RefundAmount, TotalRooms)
VALUES (@HotelB_Id, @UserId, '2025-12-01', '2025-12-01', '2026-01-12', '2026-01-15', 100.00, 1, 1, 0, 1);
INSERT INTO hotel_HotelBookings (HotelId, UserId, CreatedAt, UpdatedAt, CheckInDate, CheckOutDate, TotalPrice, PaymentStatus, Status, RefundAmount, TotalRooms)
VALUES (@HotelB_Id, @UserId, '2025-12-01', '2025-12-01', '2026-01-12', '2026-01-15', 100.00, 1, 1, 0, 1);

-- HOTEL C BOOKINGS (Week 1: Jan 19 - Jan 25)
SET @i = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO hotel_HotelBookings (HotelId, UserId, CreatedAt, UpdatedAt, CheckInDate, CheckOutDate, TotalPrice, PaymentStatus, Status, RefundAmount, TotalRooms)
    VALUES (@HotelC_Id, @UserId, '2025-12-01', '2025-12-01', '2026-01-19', '2026-01-22', 100.00, 1, 1, 0, 1);
    SET @i = @i + 1;
END

-- HOTEL C BOOKINGS (Week 2: Jan 26 - Feb 01)
SET @i = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO hotel_HotelBookings (HotelId, UserId, CreatedAt, UpdatedAt, CheckInDate, CheckOutDate, TotalPrice, PaymentStatus, Status, RefundAmount, TotalRooms)
    VALUES (@HotelC_Id, @UserId, '2025-12-01', '2025-12-01', '2026-01-26', '2026-01-29', 100.00, 1, 1, 0, 1);
    SET @i = @i + 1;
END

-- Hotel C Complaint & Fine
DECLARE @ComplaintC_Id BIGINT;
INSERT INTO hotel_Complaints (UserId, ComplaintType, HotelId, Subject, Message, Status, Priority, CreatedAt, UpdatedAt)
VALUES (@UserId, 1, @HotelC_Id, 'PAYOUT_TEST_JANUARY_2026 Complaint C', 'Test message', 1, 1, '2025-12-10', '2025-12-10');
SET @ComplaintC_Id = SCOPE_IDENTITY();

INSERT INTO admin_ProviderFines (ProviderType, ProviderId, SourceType, ComplaintId, Amount, Currency, Reason, Status, CreatedByAdminUserId, CreatedAt)
VALUES ('Hotel', @HotelC_Id, 'Complaint', @ComplaintC_Id, 30.00, 'USD', 'PAYOUT_TEST_JANUARY_2026 Fine C', 'Active', @UserId, '2025-12-10');


-- HOTEL D BOOKINGS (Week 1: Jan 12 - Jan 18)
SET @i = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO hotel_HotelBookings (HotelId, UserId, CreatedAt, UpdatedAt, CheckInDate, CheckOutDate, TotalPrice, PaymentStatus, Status, RefundAmount, TotalRooms)
    VALUES (@HotelD_Id, @UserId, '2025-12-01', '2025-12-01', '2026-01-12', '2026-01-15', 100.00, 1, 1, 0, 1);
    SET @i = @i + 1;
END

-- HOTEL D BOOKINGS (Week 2: Jan 26 - Feb 01)
SET @i = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO hotel_HotelBookings (HotelId, UserId, CreatedAt, UpdatedAt, CheckInDate, CheckOutDate, TotalPrice, PaymentStatus, Status, RefundAmount, TotalRooms)
    VALUES (@HotelD_Id, @UserId, '2025-12-01', '2025-12-01', '2026-01-26', '2026-01-29', 100.00, 1, 1, 0, 1);
    SET @i = @i + 1;
END

-- Hotel D Complaint & Fine 1
DECLARE @ComplaintD1_Id BIGINT;
INSERT INTO hotel_Complaints (UserId, ComplaintType, HotelId, Subject, Message, Status, Priority, CreatedAt, UpdatedAt)
VALUES (@UserId, 1, @HotelD_Id, 'PAYOUT_TEST_JANUARY_2026 Complaint D1', 'Test message', 1, 1, '2025-12-01', '2025-12-01');
SET @ComplaintD1_Id = SCOPE_IDENTITY();

INSERT INTO admin_ProviderFines (ProviderType, ProviderId, SourceType, ComplaintId, Amount, Currency, Reason, Status, CreatedByAdminUserId, CreatedAt)
VALUES ('Hotel', @HotelD_Id, 'Complaint', @ComplaintD1_Id, 25.00, 'USD', 'PAYOUT_TEST_JANUARY_2026 Fine D1', 'Active', @UserId, '2025-12-01');

-- Hotel D Complaint & Fine 2
DECLARE @ComplaintD2_Id BIGINT;
INSERT INTO hotel_Complaints (UserId, ComplaintType, HotelId, Subject, Message, Status, Priority, CreatedAt, UpdatedAt)
VALUES (@UserId, 1, @HotelD_Id, 'PAYOUT_TEST_JANUARY_2026 Complaint D2', 'Test message', 1, 1, '2025-12-20', '2025-12-20');
SET @ComplaintD2_Id = SCOPE_IDENTITY();

INSERT INTO admin_ProviderFines (ProviderType, ProviderId, SourceType, ComplaintId, Amount, Currency, Reason, Status, CreatedByAdminUserId, CreatedAt)
VALUES ('Hotel', @HotelD_Id, 'Complaint', @ComplaintD2_Id, 40.00, 'USD', 'PAYOUT_TEST_JANUARY_2026 Fine D2', 'Active', @UserId, '2025-12-20');

PRINT 'Hotels inserted/found: 4';
PRINT 'Bookings inserted/found: 20';
PRINT 'Refunds inserted/found: 3';
PRINT 'Complaints inserted/found: 3';
PRINT 'Fines inserted/found: 3';
PRINT 'Provider Stripe payout accounts inserted/found: 4';
PRINT 'Does NOT insert into admin_PayoutBatches or any other payout result table.';

COMMIT TRANSACTION;
