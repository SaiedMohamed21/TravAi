-- ============================================================
-- Cleanup SQL Script for Tour Guide Emergency Cancellation Flow
-- Seed Marker: TEST_TOUR_EMERGENCY_FLOW_20271231
-- ============================================================

BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. Identify target IDs into temp tables/variables for safe deletion
    -- This ensures we delete only what was created or is directly linked.
    
    DECLARE @GuideUserId BIGINT;
    SELECT @GuideUserId = Id FROM Users WHERE UserName = 'testguide_emergency_cancellation';

    DECLARE @TourGuideId BIGINT;
    SELECT @TourGuideId = Id FROM tourguide_TourGuides WHERE UserId = @GuideUserId;

    -- Store booking IDs created by the seed
    DECLARE @SeededBookings TABLE (BookingId BIGINT);
    INSERT INTO @SeededBookings (BookingId)
    SELECT Id FROM tourguide_TourBookings 
    WHERE SpecialRequests LIKE '%TEST_TOUR_EMERGENCY_FLOW_20271231%'
       OR TourGuideId = @TourGuideId;

    -- Store tour IDs created by the seed
    DECLARE @SeededTours TABLE (TourId BIGINT);
    INSERT INTO @SeededTours (TourId)
    SELECT Id FROM tourguide_Tours 
    WHERE TourTitle LIKE '%TEST_TOUR_EMERGENCY_FLOW_20271231%'
       OR TourDescription LIKE '%TEST_TOUR_EMERGENCY_FLOW_20271231%'
       OR TourGuideId = @TourGuideId;

    -- Store payment transaction IDs linked to seeded bookings
    DECLARE @SeededPaymentTransactions TABLE (PaymentTransactionId BIGINT);
    INSERT INTO @SeededPaymentTransactions (PaymentTransactionId)
    SELECT PaymentTransactionId FROM PaymentTransactionItems 
    WHERE BookingType = 'Tour' AND BookingId IN (SELECT BookingId FROM @SeededBookings);

    -- Store checkout session IDs linked to seeded bookings
    DECLARE @SeededCheckoutSessions TABLE (CheckoutSessionId BIGINT);
    INSERT INTO @SeededCheckoutSessions (CheckoutSessionId)
    SELECT CheckoutSessionId FROM PaymentTransactions 
    WHERE Id IN (SELECT PaymentTransactionId FROM @SeededPaymentTransactions);

    -- ============================================================
    -- PREVIEW SECTIONS
    -- ============================================================
    PRINT '============================================================';
    PRINT 'PREVIEW OF DATA TO BE DELETED';
    PRINT '============================================================';

    PRINT '--- Users (Tour Guide User) ---';
    SELECT Id, UserName, Email, WalletBalance FROM Users WHERE Id = @GuideUserId;

    PRINT '--- tourguide_TourGuides ---';
    SELECT Id, UserId, Name, Status FROM tourguide_TourGuides WHERE Id = @TourGuideId;

    PRINT '--- tourguide_Tours ---';
    SELECT Id, TourTitle, City, BasePriceUsd, Active FROM tourguide_Tours WHERE Id IN (SELECT TourId FROM @SeededTours);

    PRINT '--- tourguide_TourBookings ---';
    SELECT Id, UserId, TourId, TourGuideId, TotalPrice, PaymentStatus, Status FROM tourguide_TourBookings WHERE Id IN (SELECT BookingId FROM @SeededBookings);

    PRINT '--- tourguide_TourBookingParticipants ---';
    SELECT Id, BookingId, FirstName, LastName, Price FROM tourguide_TourBookingParticipants WHERE BookingId IN (SELECT BookingId FROM @SeededBookings);

    PRINT '--- tourguide_TourBookingPayments ---';
    SELECT Id, UserId, BookingId, StripePaymentIntentId, AmountPaid, Status FROM tourguide_TourBookingPayments WHERE BookingId IN (SELECT BookingId FROM @SeededBookings);

    PRINT '--- tourguide_UrgentRequests ---';
    SELECT Id, TourGuideId, TourId, Reason, Status FROM tourguide_UrgentRequests WHERE TourId IN (SELECT TourId FROM @SeededTours);

    PRINT '--- tourguide_TourBookingResolutions ---';
    SELECT Id, OriginalBookingId, UserId, ResolutionType, RefundAmount, SelectedAlternativeTourId FROM tourguide_TourBookingResolutions WHERE OriginalBookingId IN (SELECT BookingId FROM @SeededBookings);

    PRINT '--- tourguide_UserTourCompensationCoupons ---';
    SELECT Id, UserId, TriggeringBookingId, CouponCode, DiscountPercentage, IsUsed FROM tourguide_UserTourCompensationCoupons WHERE TriggeringBookingId IN (SELECT BookingId FROM @SeededBookings);

    PRINT '--- airline_WalletTransactions ---';
    SELECT Id, UserId, Amount, Type, Description, ReferenceId FROM airline_WalletTransactions 
    WHERE ReferenceId LIKE 'Refund-Tour-%' 
      AND ReferenceId IN (SELECT 'Refund-Tour-' + CAST(BookingId AS VARCHAR) FROM @SeededBookings);

    PRINT '--- PaymentTransactionItems ---';
    SELECT Id, PaymentTransactionId, BookingType, BookingId, Amount, Status FROM PaymentTransactionItems WHERE BookingType = 'Tour' AND BookingId IN (SELECT BookingId FROM @SeededBookings);

    PRINT '--- PaymentTransactions ---';
    SELECT Id, CheckoutSessionId, Provider, Amount, Status FROM PaymentTransactions WHERE Id IN (SELECT PaymentTransactionId FROM @SeededPaymentTransactions);

    PRINT '--- CheckoutSessions ---';
    SELECT Id, UserId, CheckoutType, Status, TotalAmount FROM CheckoutSessions WHERE Id IN (SELECT CheckoutSessionId FROM @SeededCheckoutSessions);

    -- ============================================================
    -- SAFE REVERSAL OF USER WALLET BALANCES
    -- ============================================================
    PRINT '============================================================';
    PRINT 'REVERSING WALLET BALANCES';
    PRINT '============================================================';

    -- If a refund was processed, we deduct the refunded amount from the user's WalletBalance.
    -- We do this by summing up the actual wallet transactions generated for each booking.
    
    DECLARE @RefundUpdates TABLE (UserId BIGINT, TotalRefund DECIMAL(18,2));
    INSERT INTO @RefundUpdates (UserId, TotalRefund)
    SELECT UserId, SUM(Amount) 
    FROM airline_WalletTransactions
    WHERE ReferenceId LIKE 'Refund-Tour-%' 
      AND ReferenceId IN (SELECT 'Refund-Tour-' + CAST(BookingId AS VARCHAR) FROM @SeededBookings)
      AND Type = 'Refund'
    GROUP BY UserId;

    IF EXISTS (SELECT 1 FROM @RefundUpdates)
    BEGIN
        PRINT 'Found wallet refunds to reverse:';
        SELECT u.Id AS UserId, u.Email, u.WalletBalance AS CurrentBalance, ru.TotalRefund AS RefundToDeduct, (u.WalletBalance - ru.TotalRefund) AS NewBalance
        FROM Users u
        JOIN @RefundUpdates ru ON u.Id = ru.UserId;

        UPDATE u
        SET u.WalletBalance = u.WalletBalance - ru.TotalRefund,
            u.UpdatedAt = GETUTCDATE()
        FROM Users u
        JOIN @RefundUpdates ru ON u.Id = ru.UserId;
        
        PRINT 'Wallet balances reversed successfully.';
    END
    ELSE
    BEGIN
        PRINT 'No wallet refunds found to reverse.';
    END

    -- ============================================================
    -- DELETION OF TEST RECORDS (CHILDREN FIRST)
    -- ============================================================
    PRINT '============================================================';
    PRINT 'PERFORMING SCOPED DELETIONS';
    PRINT '============================================================';

    -- A. Delete airline_WalletTransactions
    PRINT 'Deleting airline_WalletTransactions...';
    DELETE FROM airline_WalletTransactions
    WHERE ReferenceId LIKE 'Refund-Tour-%' 
      AND ReferenceId IN (SELECT 'Refund-Tour-' + CAST(BookingId AS VARCHAR) FROM @SeededBookings);

    -- B. Delete tourguide_UserTourCompensationCoupons
    PRINT 'Deleting tourguide_UserTourCompensationCoupons...';
    DELETE FROM tourguide_UserTourCompensationCoupons
    WHERE TriggeringBookingId IN (SELECT BookingId FROM @SeededBookings);

    -- C. Delete tourguide_TourBookingResolutions
    PRINT 'Deleting tourguide_TourBookingResolutions...';
    DELETE FROM tourguide_TourBookingResolutions
    WHERE OriginalBookingId IN (SELECT BookingId FROM @SeededBookings);

    -- D. Delete tourguide_UrgentRequests
    PRINT 'Deleting tourguide_UrgentRequests...';
    DELETE FROM tourguide_UrgentRequests
    WHERE TourId IN (SELECT TourId FROM @SeededTours);

    -- E0. Delete PaymentTransactionItems
    PRINT 'Deleting PaymentTransactionItems...';
    DELETE FROM PaymentTransactionItems
    WHERE BookingType = 'Tour' AND BookingId IN (SELECT BookingId FROM @SeededBookings);

    -- E1. Delete PaymentTransactions
    PRINT 'Deleting PaymentTransactions...';
    DELETE FROM PaymentTransactions
    WHERE Id IN (SELECT PaymentTransactionId FROM @SeededPaymentTransactions);

    -- E2. Delete CheckoutSessions
    PRINT 'Deleting CheckoutSessions...';
    DELETE FROM CheckoutSessions
    WHERE Id IN (SELECT CheckoutSessionId FROM @SeededCheckoutSessions);

    -- E. Delete tourguide_TourBookingPayments
    PRINT 'Deleting tourguide_TourBookingPayments...';
    DELETE FROM tourguide_TourBookingPayments
    WHERE BookingId IN (SELECT BookingId FROM @SeededBookings);

    -- F. Delete tourguide_TourBookingParticipants
    PRINT 'Deleting tourguide_TourBookingParticipants...';
    DELETE FROM tourguide_TourBookingParticipants
    WHERE BookingId IN (SELECT BookingId FROM @SeededBookings);

    -- G. Delete tourguide_TourBookings
    PRINT 'Deleting tourguide_TourBookings...';
    DELETE FROM tourguide_TourBookings
    WHERE Id IN (SELECT BookingId FROM @SeededBookings);

    -- H. Delete tourguide_Tours
    PRINT 'Deleting tourguide_Tours...';
    DELETE FROM tourguide_Tours
    WHERE Id IN (SELECT TourId FROM @SeededTours);

    -- I. Delete tourguide_TourGuides
    PRINT 'Deleting tourguide_TourGuides...';
    DELETE FROM tourguide_TourGuides
    WHERE Id = @TourGuideId;

    -- J. Delete Users
    PRINT 'Deleting Users (Tour Guide)...';
    DELETE FROM Users
    WHERE Id = @GuideUserId;

    COMMIT TRANSACTION;
    PRINT '============================================================';
    PRINT 'CLEANUP COMPLETED SUCCESSFULLY (TRANSACTION COMMITTED)';
    PRINT '============================================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT '============================================================';
    PRINT 'CLEANUP FAILED (TRANSACTION ROLLED BACK)';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT '============================================================';
END CATCH;
