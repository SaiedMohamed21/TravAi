-- Scripts/seed_january_airline_payout_test_data.sql

SET NOCOUNT ON;

DECLARE @AirlineA_Id BIGINT;
DECLARE @AirlineB_Id BIGINT;
DECLARE @AirlineC_Id BIGINT;
DECLARE @AirlineD_Id BIGINT;
DECLARE @UserId BIGINT = (SELECT TOP 1 Id FROM Users WHERE Role = 'Admin');
IF @UserId IS NULL SET @UserId = 1; -- Fallback

-- 1. Create Airlines (Idempotent)
IF NOT EXISTS (SELECT 1 FROM airline_Airlines WHERE Name = 'PAYOUT_TEST_JANUARY_2026_AIRLINE_A_NO_REFUND')
BEGIN
    INSERT INTO airline_Airlines (UserId, Name, Country, LogoUrl, LicenseNumber, Verified, Status, IsApproved)
    VALUES (@UserId, 'PAYOUT_TEST_JANUARY_2026_AIRLINE_A_NO_REFUND', 'TestLand', '', 'LIC-AIR-A', 1, 'Active', 1);
END
SET @AirlineA_Id = (SELECT Id FROM airline_Airlines WHERE Name = 'PAYOUT_TEST_JANUARY_2026_AIRLINE_A_NO_REFUND');

IF NOT EXISTS (SELECT 1 FROM airline_Airlines WHERE Name = 'PAYOUT_TEST_JANUARY_2026_AIRLINE_B_WITH_REFUNDS')
BEGIN
    INSERT INTO airline_Airlines (UserId, Name, Country, LogoUrl, LicenseNumber, Verified, Status, IsApproved)
    VALUES (@UserId, 'PAYOUT_TEST_JANUARY_2026_AIRLINE_B_WITH_REFUNDS', 'TestLand', '', 'LIC-AIR-B', 1, 'Active', 1);
END
SET @AirlineB_Id = (SELECT Id FROM airline_Airlines WHERE Name = 'PAYOUT_TEST_JANUARY_2026_AIRLINE_B_WITH_REFUNDS');

IF NOT EXISTS (SELECT 1 FROM airline_Airlines WHERE Name = 'PAYOUT_TEST_JANUARY_2026_AIRLINE_C_ONE_FINE')
BEGIN
    INSERT INTO airline_Airlines (UserId, Name, Country, LogoUrl, LicenseNumber, Verified, Status, IsApproved)
    VALUES (@UserId, 'PAYOUT_TEST_JANUARY_2026_AIRLINE_C_ONE_FINE', 'TestLand', '', 'LIC-AIR-C', 1, 'Active', 1);
END
SET @AirlineC_Id = (SELECT Id FROM airline_Airlines WHERE Name = 'PAYOUT_TEST_JANUARY_2026_AIRLINE_C_ONE_FINE');

IF NOT EXISTS (SELECT 1 FROM airline_Airlines WHERE Name = 'PAYOUT_TEST_JANUARY_2026_AIRLINE_D_TWO_FINES')
BEGIN
    INSERT INTO airline_Airlines (UserId, Name, Country, LogoUrl, LicenseNumber, Verified, Status, IsApproved)
    VALUES (@UserId, 'PAYOUT_TEST_JANUARY_2026_AIRLINE_D_TWO_FINES', 'TestLand', '', 'LIC-AIR-D', 1, 'Active', 1);
END
SET @AirlineD_Id = (SELECT Id FROM airline_Airlines WHERE Name = 'PAYOUT_TEST_JANUARY_2026_AIRLINE_D_TWO_FINES');

-- Helper to clear previous test bookings for clean slate
DELETE FROM airline_Bookings WHERE FlightId IN (SELECT Id FROM airline_Flights WHERE AirlineId IN (@AirlineA_Id, @AirlineB_Id, @AirlineC_Id, @AirlineD_Id));
DELETE FROM airline_Flights WHERE AirlineId IN (@AirlineA_Id, @AirlineB_Id, @AirlineC_Id, @AirlineD_Id);

-- ----------------------------------------------------------------------
-- Airline A — no refund, no fine (Week: 2026-01-05 to 2026-01-11)
-- ----------------------------------------------------------------------
DECLARE @FlightA BIGINT;
INSERT INTO airline_Flights (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime, Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber, DestinationImageUrl, Status, CreatedByUserId, FlightClass, Currency, Duration, DurationMinutes)
VALUES ('JFK', 'LHR', '2026-01-08 10:00:00', '2026-01-08 22:00:00', 100, 100, @AirlineA_Id, 0, 'TST-A-1', '', 'Completed', @UserId, 'Economy', 'USD', '12h', 720);
SET @FlightA = SCOPE_IDENTITY();

-- 5 bookings, 100 USD each, no refunds
DECLARE @i INT = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO airline_Bookings (UserId, FlightId, NumberOfSeats, TotalPrice, BookingDate, Status, RejectionReason, PaymentStatus, PassengerDetailsCompletedAt, PassengerDetailsStatus)
    VALUES (@UserId, @FlightA, 1, 100.00, '2026-01-01', 'Confirmed', NULL, 'Paid', '2026-01-01', 'Completed');
    SET @i = @i + 1;
END

-- ----------------------------------------------------------------------
-- Airline B — with refunds (Week: 2026-01-12 to 2026-01-18)
-- ----------------------------------------------------------------------
DECLARE @FlightB BIGINT;
INSERT INTO airline_Flights (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime, Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber, DestinationImageUrl, Status, CreatedByUserId, FlightClass, Currency, Duration, DurationMinutes)
VALUES ('JFK', 'LHR', '2026-01-15 10:00:00', '2026-01-15 22:00:00', 100, 100, @AirlineB_Id, 0, 'TST-B-1', '', 'Completed', @UserId, 'Economy', 'USD', '12h', 720);
SET @FlightB = SCOPE_IDENTITY();

INSERT INTO airline_Bookings (UserId, FlightId, NumberOfSeats, TotalPrice, BookingDate, Status, RejectionReason, PaymentStatus, PassengerDetailsCompletedAt, PassengerDetailsStatus)
VALUES 
(@UserId, @FlightB, 1, 100.00, '2026-01-01', 'Confirmed', NULL, 'Paid', '2026-01-01', 'Completed'),
(@UserId, @FlightB, 1, 100.00, '2026-01-01', 'Confirmed', NULL, 'Paid', '2026-01-01', 'Completed'),
(@UserId, @FlightB, 1, 20.00, '2026-01-01', 'Confirmed', 'Partial cancellation', 'Refunded', '2026-01-01', 'Completed'),
(@UserId, @FlightB, 1, 50.00, '2026-01-01', 'Confirmed', 'Partial cancellation', 'Refunded', '2026-01-01', 'Completed'),
(@UserId, @FlightB, 1, 100.00, '2026-01-01', 'Confirmed', 'Full cancellation', 'Refunded', '2026-01-01', 'Completed'),
(@UserId, @FlightB, 1, 130.00, '2026-01-01', 'Confirmed', NULL, 'Paid', '2026-01-01', 'Completed');

-- ----------------------------------------------------------------------
-- Airline C — one complaint-based fine across two weeks
-- ----------------------------------------------------------------------
DECLARE @FlightC_W1 BIGINT;
INSERT INTO airline_Flights (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime, Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber, DestinationImageUrl, Status, CreatedByUserId, FlightClass, Currency, Duration, DurationMinutes)
VALUES ('JFK', 'LHR', '2026-01-20 10:00:00', '2026-01-20 22:00:00', 100, 100, @AirlineC_Id, 0, 'TST-C-1', '', 'Completed', @UserId, 'Economy', 'USD', '12h', 720);
SET @FlightC_W1 = SCOPE_IDENTITY();

SET @i = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO airline_Bookings (UserId, FlightId, NumberOfSeats, TotalPrice, BookingDate, Status, RejectionReason, PaymentStatus, PassengerDetailsCompletedAt, PassengerDetailsStatus)
    VALUES (@UserId, @FlightC_W1, 1, 100.00, '2026-01-01', 'Confirmed', NULL, 'Paid', '2026-01-01', 'Completed');
    SET @i = @i + 1;
END

DECLARE @FlightC_W2 BIGINT;
INSERT INTO airline_Flights (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime, Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber, DestinationImageUrl, Status, CreatedByUserId, FlightClass, Currency, Duration, DurationMinutes)
VALUES ('JFK', 'LHR', '2026-01-27 10:00:00', '2026-01-27 22:00:00', 100, 100, @AirlineC_Id, 0, 'TST-C-2', '', 'Completed', @UserId, 'Economy', 'USD', '12h', 720);
SET @FlightC_W2 = SCOPE_IDENTITY();

SET @i = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO airline_Bookings (UserId, FlightId, NumberOfSeats, TotalPrice, BookingDate, Status, RejectionReason, PaymentStatus, PassengerDetailsCompletedAt, PassengerDetailsStatus)
    VALUES (@UserId, @FlightC_W2, 1, 100.00, '2026-01-01', 'Confirmed', NULL, 'Paid', '2026-01-01', 'Completed');
    SET @i = @i + 1;
END

DELETE FROM admin_ProviderFines WHERE ProviderType = 'Airline' AND ProviderId = @AirlineC_Id;

INSERT INTO admin_ProviderFines (ProviderType, ProviderId, SourceType, ComplaintId, Amount, Currency, Reason, Status, CreatedByAdminUserId, CreatedAt)
VALUES ('Airline', @AirlineC_Id, 'Complaint', NULL, 30.00, 'USD', 'Test complaint fine C', 'Active', @UserId, '2025-12-10 00:00:00');

-- ----------------------------------------------------------------------
-- Airline D — two complaint-based fines across two weeks
-- ----------------------------------------------------------------------
DECLARE @FlightD_W1 BIGINT;
INSERT INTO airline_Flights (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime, Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber, DestinationImageUrl, Status, CreatedByUserId, FlightClass, Currency, Duration, DurationMinutes)
VALUES ('JFK', 'LHR', '2026-01-15 10:00:00', '2026-01-15 22:00:00', 100, 100, @AirlineD_Id, 0, 'TST-D-1', '', 'Completed', @UserId, 'Economy', 'USD', '12h', 720);
SET @FlightD_W1 = SCOPE_IDENTITY();

SET @i = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO airline_Bookings (UserId, FlightId, NumberOfSeats, TotalPrice, BookingDate, Status, RejectionReason, PaymentStatus, PassengerDetailsCompletedAt, PassengerDetailsStatus)
    VALUES (@UserId, @FlightD_W1, 1, 100.00, '2026-01-01', 'Confirmed', NULL, 'Paid', '2026-01-01', 'Completed');
    SET @i = @i + 1;
END

DECLARE @FlightD_W2 BIGINT;
INSERT INTO airline_Flights (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime, Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber, DestinationImageUrl, Status, CreatedByUserId, FlightClass, Currency, Duration, DurationMinutes)
VALUES ('JFK', 'LHR', '2026-01-27 10:00:00', '2026-01-27 22:00:00', 100, 100, @AirlineD_Id, 0, 'TST-D-2', '', 'Completed', @UserId, 'Economy', 'USD', '12h', 720);
SET @FlightD_W2 = SCOPE_IDENTITY();

SET @i = 1;
WHILE @i <= 5
BEGIN
    INSERT INTO airline_Bookings (UserId, FlightId, NumberOfSeats, TotalPrice, BookingDate, Status, RejectionReason, PaymentStatus, PassengerDetailsCompletedAt, PassengerDetailsStatus)
    VALUES (@UserId, @FlightD_W2, 1, 100.00, '2026-01-01', 'Confirmed', NULL, 'Paid', '2026-01-01', 'Completed');
    SET @i = @i + 1;
END

DELETE FROM admin_ProviderFines WHERE ProviderType = 'Airline' AND ProviderId = @AirlineD_Id;
