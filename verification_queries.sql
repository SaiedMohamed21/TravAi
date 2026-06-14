-- Verification queries
DECLARE @Marker NVARCHAR(100) = 'TEST_TOUR_EMERGENCY_3_CASES_USER1_20271231';
DECLARE @UserId BIGINT = (SELECT Id FROM Users WHERE Email = 'user1@gmail.com');

-- 1. User Info
SELECT Id AS UserId, Email, WalletBalance FROM Users WHERE Id = @UserId;

-- 2. Pending Emergency Cancellations Bookings
SELECT b.Id AS BookingId, t.Id AS OriginalTourId, t.TourTitle AS TourName, b.TotalPrice AS OriginalPrice, b.Status, b.PaymentStatus, t.AvailableDateTime
FROM tourguide_TourBookings b
JOIN tourguide_Tours t ON b.TourId = t.Id
WHERE b.UserId = @UserId AND b.SpecialRequests LIKE @Marker + '%';

-- Original Payments
SELECT p.BookingId, p.Amount, pt.Provider, pt.PaymentMethod 
FROM PaymentTransactionItems p
JOIN PaymentTransactions pt ON p.PaymentTransactionId = pt.Id
WHERE p.BookingType = 'Tour' AND p.BookingId IN (
    SELECT Id FROM tourguide_TourBookings WHERE UserId = @UserId AND SpecialRequests LIKE @Marker + '%'
);

-- 3. Alternative Tours
SELECT Id AS AltTourId, TourTitle AS Name, City, AvailableDateTime, BasePriceUsd, Active, GroupSizeMax AS AvailableSeats
FROM tourguide_Tours
WHERE TourTitle LIKE @Marker + '_ALT%';

-- 4. Coupons
SELECT Id, CouponCode, DiscountPercentage, IsUsed, TriggeringBookingId
FROM UserTourCompensationCoupons
WHERE UserId = @UserId;
