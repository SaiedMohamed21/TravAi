-- seed_tourguide_cancelled_fine_test.sql
-- Safely selects an existing TourBooking or inserts a minimal safe dummy if none exist,
-- and updates it to simulate a Tour Guide cancellation with Admin rejection.

DECLARE @UserId bigint;
DECLARE @TourId bigint;
DECLARE @TourGuideId bigint;
DECLARE @TourBookingId bigint;

-- Get existing real IDs
SELECT TOP 1 @UserId = Id FROM Users;
SELECT TOP 1 @TourGuideId = Id FROM tourguide_TourGuides;
SELECT TOP 1 @TourId = Id FROM tourguide_Tours WHERE TourGuideId = @TourGuideId;

IF @UserId IS NOT NULL AND @TourGuideId IS NOT NULL AND @TourId IS NOT NULL
BEGIN
    -- Try to find an existing booking to update, or just create one
    SELECT TOP 1 @TourBookingId = Id FROM tourguide_TourBookings 
    WHERE TourGuideId = @TourGuideId AND TourId = @TourId;

    IF @TourBookingId IS NULL
    BEGIN
        -- Insert a safe dummy booking
        INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, ParticipantsCount, TotalPrice, Currency, PaymentStatus, Status, CreatedAt, CancelledByRole, CancellationReason, CancellationReviewStatus, CancellationReviewNotes)
        VALUES (@UserId, @TourId, @TourGuideId, GETUTCDATE(), DATEADD(day, 7, GETUTCDATE()), 1, 150.00, 'USD', 'Completed', 'Cancelled', GETUTCDATE(), 'TourGuide', 'Test cancellation for fine QA', 'Rejected', 'Unacceptable excuse for cancelling');
        
        SET @TourBookingId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        -- Update the existing booking safely
        UPDATE tourguide_TourBookings
        SET Status = 'Cancelled',
            CancelledByRole = 'TourGuide',
            CancellationReason = 'Test cancellation for fine QA',
            CancellationReviewStatus = 'Rejected',
            CancellationReviewNotes = 'Unacceptable excuse for cancelling'
        WHERE Id = @TourBookingId;
    END

    PRINT 'Successfully prepared TourBooking ID ' + CAST(@TourBookingId AS varchar) + ' for QA testing.';
END
ELSE
BEGIN
    PRINT 'Could not find required related records (User, TourGuide, Tour) to create test data.';
END
