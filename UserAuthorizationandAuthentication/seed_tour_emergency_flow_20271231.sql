-- ============================================================
-- Seed Test Bookings for Tour Guide Emergency Cancellation Flow
-- Seed Marker: TEST_TOUR_EMERGENCY_FLOW_20271231
-- Date: 2027-12-31
-- ============================================================

BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. Verify/Reuse Customers
    DECLARE @CustomerId1 BIGINT = 7; -- user1@gmail.com
    DECLARE @CustomerId2 BIGINT = 227; -- final@gmail.com

    IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = @CustomerId1) OR NOT EXISTS (SELECT 1 FROM Users WHERE Id = @CustomerId2)
    BEGIN
        THROW 50001, 'Required test users (Id=7 or Id=227) do not exist. Please run DbSeeder first.', 1;
    END

    -- 2. Create Test Tour Guide User
    DECLARE @GuideUserId BIGINT;
    INSERT INTO Users (UserName, Name, Email, PasswordHash, Role, Gender, Status, IsBanned, CreatedAt, UpdatedAt, WalletBalance, IsEmailConfirmed)
    VALUES (
        'testguide_emergency_cancellation', 
        'Test Guide Emergency', 
        'testguide_emergency@example.com', 
        'FeKw08M4keuw8e9gnsQZQgwg4yDOlMZfvIwzEkSOsiU=', -- password: 123456789
        'Tourguide', 
        'Male',
        'Active', 
        0, 
        GETUTCDATE(), 
        GETUTCDATE(), 
        0.00, 
        1
    );
    SET @GuideUserId = SCOPE_IDENTITY();

    -- Create the Tour Guide Profile
    DECLARE @TourGuideId BIGINT;
    INSERT INTO tourguide_TourGuides (UserId, Name, Bio, LicenseId, LicenseCard, Status, ExperienceYears, SuspendedUntil)
    VALUES (
        @GuideUserId, 
        'Test Guide Emergency', 
        'TEST_TOUR_EMERGENCY_FLOW_20271231 Profile Bio', 
        'LIC-20271231', 
        'LIC-CARD-20271231', 
        0, -- Active
        5, 
        NULL
    );
    SET @TourGuideId = SCOPE_IDENTITY();

    -- 3. Create Original Tour
    DECLARE @OriginalTourId BIGINT;
    INSERT INTO tourguide_Tours (
        TourGuideId, City, TourTitle, TourType, TourDescription, BasePriceUsd, Currency, 
        DurationHours, GroupSizeMax, TransportIncluded, MealsIncluded, IsAccessible, 
        Customizable, CancellationPolicy, Active, CreatedAt, AvailableDateTime
    )
    VALUES (
        @TourGuideId, 
        'Cairo', 
        'TEST_TOUR_EMERGENCY_FLOW_20271231_ORIGINAL Pyramids Special', 
        'Adventure', 
        'TEST_TOUR_EMERGENCY_FLOW_20271231 Original description', 
        100.00, 
        'USD', 
        4, 
        10, 
        1, 
        0, 
        0, 
        0, 
        24, -- CancellationPolicy.Hours24
        1, -- Active
        GETUTCDATE(), 
        '2027-12-31 09:00:00'
    );
    SET @OriginalTourId = SCOPE_IDENTITY();

    -- 4. Create 3 Alternative Tours
    DECLARE @AltTourId1 BIGINT, @AltTourId2 BIGINT, @AltTourId3 BIGINT;

    INSERT INTO tourguide_Tours (
        TourGuideId, City, TourTitle, TourType, TourDescription, BasePriceUsd, Currency, 
        DurationHours, GroupSizeMax, TransportIncluded, MealsIncluded, IsAccessible, 
        Customizable, CancellationPolicy, Active, CreatedAt, AvailableDateTime
    )
    VALUES (
        @TourGuideId, 
        'Cairo', 
        'TEST_TOUR_EMERGENCY_FLOW_20271231_ALT_1 Cairo Museum Tour', 
        'Culture', 
        'TEST_TOUR_EMERGENCY_FLOW_20271231 alternative 1 description', 
        90.00, 
        'USD', 
        3, 
        15, 
        1, 
        1, 
        1, 
        0, 
        24, 
        1, 
        GETUTCDATE(), 
        '2027-12-31 09:00:00'
    );
    SET @AltTourId1 = SCOPE_IDENTITY();

    INSERT INTO tourguide_Tours (
        TourGuideId, City, TourTitle, TourType, TourDescription, BasePriceUsd, Currency, 
        DurationHours, GroupSizeMax, TransportIncluded, MealsIncluded, IsAccessible, 
        Customizable, CancellationPolicy, Active, CreatedAt, AvailableDateTime
    )
    VALUES (
        @TourGuideId, 
        'Cairo', 
        'TEST_TOUR_EMERGENCY_FLOW_20271231_ALT_2 Nile Dinner Cruise', 
        'Entertainment', 
        'TEST_TOUR_EMERGENCY_FLOW_20271231 alternative 2 description', 
        95.00, 
        'USD', 
        3, 
        20, 
        0, 
        1, 
        0, 
        1, 
        24, 
        1, 
        GETUTCDATE(), 
        '2027-12-31 09:00:00'
    );
    SET @AltTourId2 = SCOPE_IDENTITY();

    INSERT INTO tourguide_Tours (
        TourGuideId, City, TourTitle, TourType, TourDescription, BasePriceUsd, Currency, 
        DurationHours, GroupSizeMax, TransportIncluded, MealsIncluded, IsAccessible, 
        Customizable, CancellationPolicy, Active, CreatedAt, AvailableDateTime
    )
    VALUES (
        @TourGuideId, 
        'Cairo', 
        'TEST_TOUR_EMERGENCY_FLOW_20271231_ALT_3 Khan El Khalili Bazaar', 
        'Shopping', 
        'TEST_TOUR_EMERGENCY_FLOW_20271231 alternative 3 description', 
        100.00, 
        'USD', 
        2, 
        10, 
        1, 
        0, 
        0, 
        0, 
        24, 
        1, 
        GETUTCDATE(), 
        '2027-12-31 09:00:00'
    );
    SET @AltTourId3 = SCOPE_IDENTITY();

    -- 5. Create Paid Active Bookings on Original Tour
    DECLARE @BookingId1 BIGINT, @BookingId2 BIGINT;

    INSERT INTO tourguide_TourBookings (
        UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, 
        ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt
    )
    VALUES (
        @CustomerId1, 
        @OriginalTourId, 
        @TourGuideId, 
        GETUTCDATE(), 
        '2027-12-31', 
        '09:00:00', 
        1, 
        100.00, 
        'USD', 
        'TEST_TOUR_EMERGENCY_FLOW_20271231 booking 1 requests', 
        'Completed', -- Paid status expected by code
        'Confirmed', -- Active status expected by code
        GETUTCDATE()
    );
    SET @BookingId1 = SCOPE_IDENTITY();

    INSERT INTO tourguide_TourBookings (
        UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, 
        ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt
    )
    VALUES (
        @CustomerId2, 
        @OriginalTourId, 
        @TourGuideId, 
        GETUTCDATE(), 
        '2027-12-31', 
        '09:00:00', 
        1, 
        100.00, 
        'USD', 
        'TEST_TOUR_EMERGENCY_FLOW_20271231 booking 2 requests', 
        'Completed', 
        'Confirmed', 
        GETUTCDATE()
    );
    SET @BookingId2 = SCOPE_IDENTITY();

    -- 6. Add Booking Participants
    INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, LastName, DateOfBirth, Gender, Nationality, Price)
    VALUES 
        (@BookingId1, 'Adult', 'Adult', 'John', 'TEST_EMERGENCY_FLOW_1', '1990-01-01', 'Male', 'US', 100.00),
        (@BookingId2, 'Adult', 'Adult', 'Jane', 'TEST_EMERGENCY_FLOW_2', '1995-05-05', 'Female', 'UK', 100.00);

    -- 7. Add Booking Payments (to represent real paid transactions)
    INSERT INTO tourguide_TourBookingPayments (
        UserId, BookingId, StripePaymentIntentId, AmountPaid, Currency, 
        Status, PlatformCommission, ProviderNetAmount, PayoutStatus, CreatedAt
    )
    VALUES 
        (@CustomerId1, @BookingId1, 'pi_test_emergency_1', 100.00, 'USD', 'Completed', 10.00, 90.00, 'Pending', GETUTCDATE()),
        (@CustomerId2, @BookingId2, 'pi_test_emergency_2', 100.00, 'USD', 'Completed', 10.00, 90.00, 'Pending', GETUTCDATE());

    -- 8. Add Unified Checkout Sessions & Payment Transactions
    DECLARE @SessionId1 BIGINT, @SessionId2 BIGINT;
    DECLARE @PaymentTransId1 BIGINT, @PaymentTransId2 BIGINT;

    INSERT INTO CheckoutSessions (UserId, CheckoutType, Status, TotalAmount, Currency, ExpiresAt, CreatedAt)
    VALUES (@CustomerId1, 'Tour', 'Completed', 100.00, 'USD', GETUTCDATE(), GETUTCDATE());
    SET @SessionId1 = SCOPE_IDENTITY();

    INSERT INTO CheckoutSessions (UserId, CheckoutType, Status, TotalAmount, Currency, ExpiresAt, CreatedAt)
    VALUES (@CustomerId2, 'Tour', 'Completed', 100.00, 'USD', GETUTCDATE(), GETUTCDATE());
    SET @SessionId2 = SCOPE_IDENTITY();

    INSERT INTO PaymentTransactions (CheckoutSessionId, Provider, ProviderTransactionId, Amount, Currency, Status, PaidAt, CreatedAt, PaymentMethod, StripePaymentIntentId, TotalAmount, UpdatedAt, UserId)
    VALUES (@SessionId1, 'Wallet', 'wallet_tx_78', 100.00, 'USD', 'Completed', GETUTCDATE(), GETUTCDATE(), 'Wallet', NULL, 100.00, GETUTCDATE(), @CustomerId1);
    SET @PaymentTransId1 = SCOPE_IDENTITY();

    INSERT INTO PaymentTransactions (CheckoutSessionId, Provider, ProviderTransactionId, Amount, Currency, Status, PaidAt, CreatedAt, PaymentMethod, StripePaymentIntentId, TotalAmount, UpdatedAt, UserId)
    VALUES (@SessionId2, 'Stripe', 'pi_test_emergency_2', 100.00, 'USD', 'Completed', GETUTCDATE(), GETUTCDATE(), 'Stripe', 'pi_test_emergency_2', 100.00, GETUTCDATE(), @CustomerId2);
    SET @PaymentTransId2 = SCOPE_IDENTITY();

    INSERT INTO PaymentTransactionItems (PaymentTransactionId, BookingType, BookingId, Amount, Currency, Status, CreatedAt)
    VALUES (@PaymentTransId1, 'Tour', @BookingId1, 100.00, 'USD', 'Paid', GETUTCDATE());

    INSERT INTO PaymentTransactionItems (PaymentTransactionId, BookingType, BookingId, Amount, Currency, Status, CreatedAt)
    VALUES (@PaymentTransId2, 'Tour', @BookingId2, 100.00, 'USD', 'Paid', GETUTCDATE());

    COMMIT TRANSACTION;

    -- Print Created IDs for final report
    SELECT 
        'SUCCESS' AS SeedStatus,
        @GuideUserId AS GuideUserId,
        @TourGuideId AS TourGuideId,
        @OriginalTourId AS OriginalTourId,
        @AltTourId1 AS AltTourId1,
        @AltTourId2 AS AltTourId2,
        @AltTourId3 AS AltTourId3,
        @BookingId1 AS BookingId1,
        @BookingId2 AS BookingId2;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    SELECT 
        'FAILED' AS SeedStatus,
        ERROR_MESSAGE() AS ErrorMessage;
END CATCH;
