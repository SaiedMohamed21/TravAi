-- Scripts/seed_january_tour_payout_test_data.sql

SET NOCOUNT ON;

DECLARE @TourA_Id BIGINT;
DECLARE @TourB_Id BIGINT;
DECLARE @TourC_Id BIGINT;
DECLARE @TourD_Id BIGINT;
DECLARE @UserId BIGINT = (SELECT TOP 1 Id FROM Users WHERE Role = 'Admin');
IF @UserId IS NULL SET @UserId = 1;

-- 1. Create TourGuides (Idempotent)
IF NOT EXISTS (SELECT 1 FROM tourguide_TourGuides WHERE Name = 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_A_NO_REFUND')
BEGIN
    INSERT INTO tourguide_TourGuides (UserId, Name, Bio, LicenseId, LicenseCard, LicenseIdFrontPhoto, LicenseIdBackPhoto, Certification, Status, ExperienceYears, SuspendedUntil)
    VALUES (@UserId, 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_A_NO_REFUND', 'Bio', 'LIC-T-A', '', '', '', '', 1, 5, '2000-01-01');
END
SET @TourA_Id = (SELECT Id FROM tourguide_TourGuides WHERE Name = 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_A_NO_REFUND');

IF NOT EXISTS (SELECT 1 FROM tourguide_TourGuides WHERE Name = 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_B_WITH_REFUNDS')
BEGIN
    INSERT INTO tourguide_TourGuides (UserId, Name, Bio, LicenseId, LicenseCard, LicenseIdFrontPhoto, LicenseIdBackPhoto, Certification, Status, ExperienceYears, SuspendedUntil)
    VALUES (@UserId, 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_B_WITH_REFUNDS', 'Bio', 'LIC-T-B', '', '', '', '', 1, 5, '2000-01-01');
END
SET @TourB_Id = (SELECT Id FROM tourguide_TourGuides WHERE Name = 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_B_WITH_REFUNDS');

IF NOT EXISTS (SELECT 1 FROM tourguide_TourGuides WHERE Name = 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_C_ONE_FINE')
BEGIN
    INSERT INTO tourguide_TourGuides (UserId, Name, Bio, LicenseId, LicenseCard, LicenseIdFrontPhoto, LicenseIdBackPhoto, Certification, Status, ExperienceYears, SuspendedUntil)
    VALUES (@UserId, 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_C_ONE_FINE', 'Bio', 'LIC-T-C', '', '', '', '', 1, 5, '2000-01-01');
END
SET @TourC_Id = (SELECT Id FROM tourguide_TourGuides WHERE Name = 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_C_ONE_FINE');

IF NOT EXISTS (SELECT 1 FROM tourguide_TourGuides WHERE Name = 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_D_TWO_FINES')
BEGIN
    INSERT INTO tourguide_TourGuides (UserId, Name, Bio, LicenseId, LicenseCard, LicenseIdFrontPhoto, LicenseIdBackPhoto, Certification, Status, ExperienceYears, SuspendedUntil)
    VALUES (@UserId, 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_D_TWO_FINES', 'Bio', 'LIC-T-D', '', '', '', '', 1, 5, '2000-01-01');
END
SET @TourD_Id = (SELECT Id FROM tourguide_TourGuides WHERE Name = 'PAYOUT_TEST_JANUARY_2026_TOURGUIDE_D_TWO_FINES');

-- Helper to clear previous test bookings for clean slate
DELETE FROM tourguide_TourBookingResolutions WHERE OriginalBookingId IN (SELECT Id FROM tourguide_TourBookings WHERE TourGuideId IN (@TourA_Id, @TourB_Id, @TourC_Id, @TourD_Id));
DELETE FROM tourguide_TourBookings WHERE TourGuideId IN (@TourA_Id, @TourB_Id, @TourC_Id, @TourD_Id);
DELETE FROM tourguide_Tours WHERE TourGuideId IN (@TourA_Id, @TourB_Id, @TourC_Id, @TourD_Id);

-- ----------------------------------------------------------------------
-- TourGuide A — no refund, no fine (Week: 2026-01-05 to 2026-01-11)
-- ----------------------------------------------------------------------
DECLARE @TourA_TId BIGINT;
INSERT INTO tourguide_Tours (TourGuideId, TourTitle, Currency, TransportIncluded, MealsIncluded, IsAccessible, Customizable, CancellationPolicy, Active, CreatedAt)
VALUES (@TourA_Id, 'Tour A', 'USD', 0, 0, 0, 0, 1, 1, GETUTCDATE());
SET @TourA_TId = SCOPE_IDENTITY();

DECLARE @i INT = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt)
    VALUES (@UserId, @TourA_TId, @TourA_Id, '2026-01-01', '2026-01-08', '10:00:00', 1, 100.00, 'USD', '', 'Completed', 'Confirmed', GETUTCDATE());
    SET @i = @i + 1;
END

-- ----------------------------------------------------------------------
-- TourGuide B — with refunds (Week: 2026-01-12 to 2026-01-18)
-- ----------------------------------------------------------------------
DECLARE @TourB_TId BIGINT;
INSERT INTO tourguide_Tours (TourGuideId, TourTitle, Currency, TransportIncluded, MealsIncluded, IsAccessible, Customizable, CancellationPolicy, Active, CreatedAt)
VALUES (@TourB_Id, 'Tour B', 'USD', 0, 0, 0, 0, 1, 1, GETUTCDATE());
SET @TourB_TId = SCOPE_IDENTITY();

DECLARE @B_Booking1 BIGINT, @B_Booking2 BIGINT, @B_Booking3 BIGINT, @B_Booking4 BIGINT, @B_Booking5 BIGINT;

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt)
VALUES (@UserId, @TourB_TId, @TourB_Id, '2026-01-01', '2026-01-15', '10:00:00', 1, 100.00, 'USD', '', 'Completed', 'Confirmed', GETUTCDATE());
SET @B_Booking1 = SCOPE_IDENTITY();

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt)
VALUES (@UserId, @TourB_TId, @TourB_Id, '2026-01-01', '2026-01-15', '10:00:00', 1, 100.00, 'USD', '', 'Completed', 'Confirmed', GETUTCDATE());
SET @B_Booking2 = SCOPE_IDENTITY();

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt)
VALUES (@UserId, @TourB_TId, @TourB_Id, '2026-01-01', '2026-01-15', '10:00:00', 1, 100.00, 'USD', '', 'Completed', 'Confirmed', GETUTCDATE());
SET @B_Booking3 = SCOPE_IDENTITY();

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt)
VALUES (@UserId, @TourB_TId, @TourB_Id, '2026-01-01', '2026-01-15', '10:00:00', 1, 100.00, 'USD', '', 'Completed', 'Confirmed', GETUTCDATE());
SET @B_Booking4 = SCOPE_IDENTITY();

INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt)
VALUES (@UserId, @TourB_TId, @TourB_Id, '2026-01-01', '2026-01-15', '10:00:00', 1, 100.00, 'USD', '', 'Completed', 'Confirmed', GETUTCDATE());
SET @B_Booking5 = SCOPE_IDENTITY();

-- Apply refunds via tourguide_TourBookingResolutions
INSERT INTO tourguide_TourBookingResolutions (OriginalBookingId, UserId, ResolutionType, RefundAmount, ResolvedAt)
VALUES
(@B_Booking3, @UserId, 'Refund', 20.00, GETUTCDATE()),
(@B_Booking4, @UserId, 'Refund', 50.00, GETUTCDATE()),
(@B_Booking5, @UserId, 'Refund', 100.00, GETUTCDATE());

-- ----------------------------------------------------------------------
-- TourGuide C — one complaint-based fine across two weeks
-- ----------------------------------------------------------------------
DECLARE @TourC_T1 BIGINT;
INSERT INTO tourguide_Tours (TourGuideId, TourTitle, Currency, TransportIncluded, MealsIncluded, IsAccessible, Customizable, CancellationPolicy, Active, CreatedAt)
VALUES (@TourC_Id, 'Tour C 1', 'USD', 0, 0, 0, 0, 1, 1, GETUTCDATE());
SET @TourC_T1 = SCOPE_IDENTITY();

SET @i = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt)
    VALUES (@UserId, @TourC_T1, @TourC_Id, '2026-01-01', '2026-01-20', '10:00:00', 1, 100.00, 'USD', '', 'Completed', 'Confirmed', GETUTCDATE());
    SET @i = @i + 1;
END

DECLARE @TourC_T2 BIGINT;
INSERT INTO tourguide_Tours (TourGuideId, TourTitle, Currency, TransportIncluded, MealsIncluded, IsAccessible, Customizable, CancellationPolicy, Active, CreatedAt)
VALUES (@TourC_Id, 'Tour C 2', 'USD', 0, 0, 0, 0, 1, 1, GETUTCDATE());
SET @TourC_T2 = SCOPE_IDENTITY();

SET @i = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt)
    VALUES (@UserId, @TourC_T2, @TourC_Id, '2026-01-01', '2026-01-27', '10:00:00', 1, 100.00, 'USD', '', 'Completed', 'Confirmed', GETUTCDATE());
    SET @i = @i + 1;
END

DELETE FROM admin_ProviderFines WHERE ProviderType = 'TourGuide' AND ProviderId = @TourC_Id;

INSERT INTO admin_ProviderFines (ProviderType, ProviderId, SourceType, ComplaintId, Amount, Currency, Reason, Status, CreatedByAdminUserId, CreatedAt)
VALUES ('TourGuide', @TourC_Id, 'Complaint', NULL, 30.00, 'USD', 'Test complaint fine C', 'Active', @UserId, '2025-12-10 00:00:00');

-- ----------------------------------------------------------------------
-- TourGuide D — two complaint-based fines across two weeks
-- ----------------------------------------------------------------------
DECLARE @TourD_T1 BIGINT;
INSERT INTO tourguide_Tours (TourGuideId, TourTitle, Currency, TransportIncluded, MealsIncluded, IsAccessible, Customizable, CancellationPolicy, Active, CreatedAt)
VALUES (@TourD_Id, 'Tour D 1', 'USD', 0, 0, 0, 0, 1, 1, GETUTCDATE());
SET @TourD_T1 = SCOPE_IDENTITY();

SET @i = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt)
    VALUES (@UserId, @TourD_T1, @TourD_Id, '2026-01-01', '2026-01-15', '10:00:00', 1, 100.00, 'USD', '', 'Completed', 'Confirmed', GETUTCDATE());
    SET @i = @i + 1;
END

DECLARE @TourD_T2 BIGINT;
INSERT INTO tourguide_Tours (TourGuideId, TourTitle, Currency, TransportIncluded, MealsIncluded, IsAccessible, Customizable, CancellationPolicy, Active, CreatedAt)
VALUES (@TourD_Id, 'Tour D 2', 'USD', 0, 0, 0, 0, 1, 1, GETUTCDATE());
SET @TourD_T2 = SCOPE_IDENTITY();

SET @i = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt)
    VALUES (@UserId, @TourD_T2, @TourD_Id, '2026-01-01', '2026-01-27', '10:00:00', 1, 100.00, 'USD', '', 'Completed', 'Confirmed', GETUTCDATE());
    SET @i = @i + 1;
END

DELETE FROM admin_ProviderFines WHERE ProviderType = 'TourGuide' AND ProviderId = @TourD_Id;
