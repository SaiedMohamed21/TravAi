-- ============================================================
-- Seed Test Bookings for tourguide.test@demo.com
-- Run this script in Azure Query Editor or SSMS
-- ============================================================

-- Step 1: Find the User ID and TourGuide ID for tourguide.test@demo.com
DECLARE @UserId BIGINT;
DECLARE @TourGuideId BIGINT;

SELECT @UserId = Id FROM Users WHERE Email = 'tourguide.test@demo.com';
SELECT @TourGuideId = Id FROM tourguide_TourGuides WHERE UserId = @UserId;

-- Debug: Print the IDs found
PRINT 'UserId: ' + ISNULL(CAST(@UserId AS NVARCHAR), 'NOT FOUND');
PRINT 'TourGuideId: ' + ISNULL(CAST(@TourGuideId AS NVARCHAR), 'NOT FOUND');

-- Step 2: Get tour IDs owned by this tour guide
DECLARE @TourId1 BIGINT, @TourId2 BIGINT, @TourId3 BIGINT, @TourId4 BIGINT, @TourId5 BIGINT;

SELECT TOP 5
    @TourId1 = CASE WHEN ROW_NUMBER() OVER (ORDER BY Id) = 1 THEN Id ELSE @TourId1 END,
    @TourId2 = CASE WHEN ROW_NUMBER() OVER (ORDER BY Id) = 2 THEN Id ELSE @TourId2 END,
    @TourId3 = CASE WHEN ROW_NUMBER() OVER (ORDER BY Id) = 3 THEN Id ELSE @TourId3 END,
    @TourId4 = CASE WHEN ROW_NUMBER() OVER (ORDER BY Id) = 4 THEN Id ELSE @TourId4 END,
    @TourId5 = CASE WHEN ROW_NUMBER() OVER (ORDER BY Id) = 5 THEN Id ELSE @TourId5 END
FROM tourguide_Tours WHERE TourGuideId = @TourGuideId;

-- If tour guide has no tours, use any active tours and assign this tour guide
IF @TourId1 IS NULL
BEGIN
    SELECT TOP 5
        @TourId1 = CASE WHEN rn = 1 THEN Id ELSE @TourId1 END,
        @TourId2 = CASE WHEN rn = 2 THEN Id ELSE @TourId2 END,
        @TourId3 = CASE WHEN rn = 3 THEN Id ELSE @TourId3 END,
        @TourId4 = CASE WHEN rn = 4 THEN Id ELSE @TourId4 END,
        @TourId5 = CASE WHEN rn = 5 THEN Id ELSE @TourId5 END
    FROM (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn
        FROM tourguide_Tours WHERE Active = 1
    ) t
    WHERE rn <= 5;
END;

-- Fallback: use @TourId1 for any NULL tour IDs
SET @TourId2 = ISNULL(@TourId2, @TourId1);
SET @TourId3 = ISNULL(@TourId3, @TourId1);
SET @TourId4 = ISNULL(@TourId4, @TourId1);
SET @TourId5 = ISNULL(@TourId5, @TourId1);

PRINT 'TourId1: ' + ISNULL(CAST(@TourId1 AS NVARCHAR), 'NOT FOUND');
PRINT 'TourId2: ' + ISNULL(CAST(@TourId2 AS NVARCHAR), 'NOT FOUND');

-- Step 3: Find a few test user IDs (not the tour guide) to act as "customers"
DECLARE @CustomerId1 BIGINT, @CustomerId2 BIGINT, @CustomerId3 BIGINT;

SELECT TOP 3
    @CustomerId1 = CASE WHEN rn = 1 THEN Id ELSE @CustomerId1 END,
    @CustomerId2 = CASE WHEN rn = 2 THEN Id ELSE @CustomerId2 END,
    @CustomerId3 = CASE WHEN rn = 3 THEN Id ELSE @CustomerId3 END
FROM (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn
    FROM Users WHERE Id != @UserId
) u
WHERE rn <= 3;

-- Fallback: if no other users, use the tour guide's own ID
SET @CustomerId1 = ISNULL(@CustomerId1, @UserId);
SET @CustomerId2 = ISNULL(@CustomerId2, @UserId);
SET @CustomerId3 = ISNULL(@CustomerId3, @UserId);

-- ============================================================
-- Step 4: Insert Test Bookings (6 bookings with different statuses)
-- ============================================================

-- Booking 1: CONFIRMED - Upcoming tour (Pyramids Tour)
INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt)
VALUES (
    @CustomerId1, @TourId1, @TourGuideId,
    DATEADD(DAY, -3, GETUTCDATE()),           -- Booked 3 days ago
    DATEADD(DAY, 7, GETUTCDATE()),             -- Tour is 7 days from now
    '09:00:00',                                 -- 9 AM
    2,                                          -- 2 participants
    150.00, 'USD',
    'We need a wheelchair accessible vehicle.',
    'Completed', 'Confirmed',
    DATEADD(DAY, -3, GETUTCDATE())
);

-- Booking 2: CONFIRMED - Another upcoming tour
INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt)
VALUES (
    @CustomerId2, @TourId2, @TourGuideId,
    DATEADD(DAY, -5, GETUTCDATE()),
    DATEADD(DAY, 14, GETUTCDATE()),
    '10:30:00',
    4,
    320.00, 'USD',
    'Vegetarian meals preferred.',
    'Completed', 'Confirmed',
    DATEADD(DAY, -5, GETUTCDATE())
);

-- Booking 3: PENDING - Awaiting payment
INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt)
VALUES (
    @CustomerId3, @TourId3, @TourGuideId,
    DATEADD(DAY, -1, GETUTCDATE()),
    DATEADD(DAY, 10, GETUTCDATE()),
    '08:00:00',
    1,
    75.00, 'USD',
    NULL,
    'Pending', 'Pending',
    DATEADD(DAY, -1, GETUTCDATE())
);

-- Booking 4: PENDING - Another pending booking
INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt)
VALUES (
    @CustomerId1, @TourId4, @TourGuideId,
    GETUTCDATE(),
    DATEADD(DAY, 21, GETUTCDATE()),
    '14:00:00',
    3,
    225.00, 'USD',
    'Children ages 8 and 10 joining.',
    'Pending', 'Pending',
    GETUTCDATE()
);

-- Booking 5: CANCELLED - Was confirmed but cancelled
INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt, UpdatedAt)
VALUES (
    @CustomerId2, @TourId5, @TourGuideId,
    DATEADD(DAY, -10, GETUTCDATE()),
    DATEADD(DAY, 3, GETUTCDATE()),
    '11:00:00',
    2,
    180.00, 'USD',
    'Plans changed, need to cancel.',
    'Refunded', 'Cancelled',
    DATEADD(DAY, -10, GETUTCDATE()),
    DATEADD(DAY, -2, GETUTCDATE())
);

-- Booking 6: COMPLETED - Past tour that was completed
INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, TourTime, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt, UpdatedAt)
VALUES (
    @CustomerId3, @TourId1, @TourGuideId,
    DATEADD(DAY, -30, GETUTCDATE()),
    DATEADD(DAY, -15, GETUTCDATE()),
    '09:00:00',
    2,
    150.00, 'USD',
    'Great experience! Thank you.',
    'Completed', 'Completed',
    DATEADD(DAY, -30, GETUTCDATE()),
    DATEADD(DAY, -15, GETUTCDATE())
);

-- ============================================================
-- Step 5: Add Participants for the Confirmed bookings
-- ============================================================

-- Get the IDs of the bookings we just inserted
DECLARE @BookingId1 BIGINT, @BookingId2 BIGINT, @BookingId6 BIGINT;

SELECT TOP 1 @BookingId1 = Id FROM tourguide_TourBookings 
WHERE TourGuideId = @TourGuideId AND Status = 'Confirmed' AND ParticipantsCount = 2
ORDER BY CreatedAt DESC;

SELECT TOP 1 @BookingId2 = Id FROM tourguide_TourBookings 
WHERE TourGuideId = @TourGuideId AND Status = 'Confirmed' AND ParticipantsCount = 4
ORDER BY CreatedAt DESC;

SELECT TOP 1 @BookingId6 = Id FROM tourguide_TourBookings 
WHERE TourGuideId = @TourGuideId AND Status = 'Completed'
ORDER BY CreatedAt DESC;

-- Participants for Booking 1 (2 participants)
IF @BookingId1 IS NOT NULL
BEGIN
    INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, LastName, DateOfBirth, Gender, Nationality, Price)
    VALUES 
        (@BookingId1, 'Adult', 'Adult', 'Ahmed', 'Hassan', '1990-05-15', 'Male', 'Egyptian', 75.00),
        (@BookingId1, 'Adult', 'Adult', 'Sara', 'Mohamed', '1992-08-20', 'Female', 'Egyptian', 75.00);
END

-- Participants for Booking 2 (4 participants)
IF @BookingId2 IS NOT NULL
BEGIN
    INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, LastName, DateOfBirth, Gender, Nationality, Price)
    VALUES 
        (@BookingId2, 'Adult', 'Adult', 'John', 'Smith', '1985-03-10', 'Male', 'American', 80.00),
        (@BookingId2, 'Adult', 'Adult', 'Emily', 'Smith', '1987-11-22', 'Female', 'American', 80.00),
        (@BookingId2, 'Child', 'Child', 'Tom', 'Smith', '2015-06-01', 'Male', 'American', 80.00),
        (@BookingId2, 'Child', 'Child', 'Lily', 'Smith', '2017-09-14', 'Female', 'American', 80.00);
END

-- Participants for Booking 6 (2 participants - completed)
IF @BookingId6 IS NOT NULL
BEGIN
    INSERT INTO tourguide_TourBookingParticipants (BookingId, ParticipantType, AgeType, FirstName, LastName, DateOfBirth, Gender, Nationality, Price)
    VALUES 
        (@BookingId6, 'Adult', 'Adult', 'Omar', 'Ali', '1988-01-25', 'Male', 'Egyptian', 75.00),
        (@BookingId6, 'Adult', 'Adult', 'Nour', 'Ali', '1991-04-18', 'Female', 'Egyptian', 75.00);
END

-- ============================================================
-- Step 6: Verify the data
-- ============================================================
SELECT 
    b.Id, 
    b.Status, 
    b.PaymentStatus,
    b.TotalPrice, 
    b.ParticipantsCount,
    b.TourDate,
    t.TourTitle,
    u.Email AS CustomerEmail,
    tg.Name AS TourGuideName
FROM tourguide_TourBookings b
JOIN tourguide_Tours t ON b.TourId = t.Id
JOIN Users u ON b.UserId = u.Id
JOIN tourguide_TourGuides tg ON b.TourGuideId = tg.Id
WHERE b.TourGuideId = @TourGuideId
ORDER BY b.CreatedAt DESC;

PRINT '✅ Test bookings seeded successfully!';
