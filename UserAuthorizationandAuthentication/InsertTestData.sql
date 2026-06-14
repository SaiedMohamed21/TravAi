BEGIN TRAN;

-- 1. Insert HotelBooking
INSERT INTO hotel_HotelBookings (UserId, HotelId, CheckInDate, CheckOutDate, Nights, TotalRooms, TotalPrice, PaymentStatus, Status, CreatedAt, UpdatedAt, CancellationReason)
VALUES (227, 242, '2026-06-10', '2026-06-11', 1, 1, 150.00, 1, 6, GETUTCDATE(), GETUTCDATE(), 'TEST_PAST_FLOW_227_20260610');

DECLARE @HotelBookingId INT = SCOPE_IDENTITY();

INSERT INTO hotel_HotelBookingRooms (BookingId, RoomId, RoomName, MealPlan, PricePerNight, Nights, Subtotal)
VALUES (@HotelBookingId, 484, 'Test Room', 'RO', 150.00, 1, 150.00);

-- 2. Insert AirlineBooking
INSERT INTO airline_Bookings (UserId, FlightId, NumberOfSeats, TotalPrice, BookingDate, Status, RejectionReason, PaymentStatus, PassengerDetailsStatus, PassengerDetailsCompletedAt)
VALUES (227, 29971, 1, 300.00, '2026-06-10', 'Approved', 'TEST_PAST_FLOW_227_20260610', 'Paid', 'Complete', GETUTCDATE());

DECLARE @AirlineBookingId INT = SCOPE_IDENTITY();

-- 3. Insert TourBooking
INSERT INTO tourguide_TourBookings (UserId, TourId, TourGuideId, BookingDate, TourDate, ParticipantsCount, TotalPrice, Currency, SpecialRequests, PaymentStatus, Status, CreatedAt, UpdatedAt)
VALUES (227, 5052, 150, GETUTCDATE(), '2026-06-10', 1, 200.00, 'USD', 'TEST_PAST_FLOW_227_20260610', 'Paid', 'Completed', GETUTCDATE(), GETUTCDATE());

DECLARE @TourBookingId INT = SCOPE_IDENTITY();

SELECT @HotelBookingId AS HotelBookingId, @AirlineBookingId AS AirlineBookingId, @TourBookingId AS TourBookingId;

COMMIT TRAN;
