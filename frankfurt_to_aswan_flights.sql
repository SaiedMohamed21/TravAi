-- ============================================================
--  Frankfurt → Aswan  |  27 May 2026  |  Multiple Flights
-- ============================================================

-- 1. Airports  (skip if already exist)
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM airline_Airports WHERE Code = 'FRA')
INSERT INTO airline_Airports (Code, Name, City, Country)
VALUES ('FRA', 'Frankfurt Airport', 'Frankfurt', 'Germany');

IF NOT EXISTS (SELECT 1 FROM airline_Airports WHERE Code = 'ASW')
INSERT INTO airline_Airports (Code, Name, City, Country)
VALUES ('ASW', 'Aswan International Airport', 'Aswan', 'Egypt');

-- 2. Airline  (skip if already exists)
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM airline_Airlines WHERE Name = 'EgyptAir')
INSERT INTO airline_Airlines (Name, Country, LicenseNumber, IsApproved, Verified, Status, UserId)
VALUES ('EgyptAir', 'Egypt', 'MS-001', 1, 1, 'Active', NULL);

DECLARE @airlineId BIGINT = (SELECT TOP 1 Id FROM airline_Airlines WHERE Name = 'EgyptAir');

-- 3. Flights Frankfurt → Aswan  |  27/05/2026
-- ------------------------------------------------------------
-- Flight 1 – Economy  |  $320  |  07:00 → 13:30  (6h 30m)
INSERT INTO airline_Flights
  (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime,
   Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber,
   FlightClass, Currency, Duration, DurationMinutes, Status)
VALUES
  ('FRA', 'ASW', '2026-05-27 07:00:00', '2026-05-27 13:30:00',
   320.00, 150, @airlineId, 0, 'MS-701',
   'Economy', 'USD', '6h 30m', 390, 'Active');

-- Flight 2 – Economy  |  $410  |  10:00 → 16:45  (6h 45m, 1 stop)
INSERT INTO airline_Flights
  (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime,
   Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber,
   FlightClass, Currency, Duration, DurationMinutes, Status)
VALUES
  ('FRA', 'ASW', '2026-05-27 10:00:00', '2026-05-27 16:45:00',
   410.00, 120, @airlineId, 1, 'MS-703',
   'Economy', 'USD', '6h 45m', 405, 'Active');

-- Flight 3 – Economy  |  $275  |  14:30 → 21:00  (6h 30m)
INSERT INTO airline_Flights
  (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime,
   Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber,
   FlightClass, Currency, Duration, DurationMinutes, Status)
VALUES
  ('FRA', 'ASW', '2026-05-27 14:30:00', '2026-05-27 21:00:00',
   275.00, 200, @airlineId, 0, 'MS-705',
   'Economy', 'USD', '6h 30m', 390, 'Active');

-- Flight 4 – Business  |  $850  |  08:00 → 14:30  (6h 30m)
INSERT INTO airline_Flights
  (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime,
   Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber,
   FlightClass, Currency, Duration, DurationMinutes, Status)
VALUES
  ('FRA', 'ASW', '2026-05-27 08:00:00', '2026-05-27 14:30:00',
   850.00, 40, @airlineId, 0, 'MS-707',
   'Business', 'USD', '6h 30m', 390, 'Active');

-- Flight 5 – First Class  |  $1500  |  09:00 → 15:30  (6h 30m)
INSERT INTO airline_Flights
  (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime,
   Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber,
   FlightClass, Currency, Duration, DurationMinutes, Status)
VALUES
  ('FRA', 'ASW', '2026-05-27 09:00:00', '2026-05-27 15:30:00',
   1500.00, 20, @airlineId, 0, 'MS-709',
   'First', 'USD', '6h 30m', 390, 'Active');

-- ✅ Verify Go flights:
SELECT f.Id, f.FlightNumber, f.FlightClass, f.Price,
       f.DepartureTime, f.ArrivalTime, f.Duration,
       f.NumberOfStops, f.AvailableSeats
FROM airline_Flights f
WHERE f.DepartureAirportCode = 'FRA'
  AND f.ArrivalAirportCode   = 'ASW'
  AND CAST(f.DepartureTime AS DATE) = '2026-05-27'
ORDER BY f.Price;

-- ============================================================
--  Aswan → Frankfurt  |  30 May 2026  |  Return Flights
-- ============================================================

DECLARE @airlineIdRet BIGINT = (SELECT TOP 1 Id FROM airline_Airlines WHERE Name = 'EgyptAir');

-- Return Flight 1 – Economy  |  $295  |  06:00 → 12:30  (6h 30m)
INSERT INTO airline_Flights
  (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime,
   Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber,
   FlightClass, Currency, Duration, DurationMinutes, Status)
VALUES
  ('ASW', 'FRA', '2026-05-30 06:00:00', '2026-05-30 12:30:00',
   295.00, 160, @airlineIdRet, 0, 'MS-702',
   'Economy', 'USD', '6h 30m', 390, 'Active');

-- Return Flight 2 – Economy  |  $380  |  09:30 → 16:15  (6h 45m, 1 stop)
INSERT INTO airline_Flights
  (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime,
   Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber,
   FlightClass, Currency, Duration, DurationMinutes, Status)
VALUES
  ('ASW', 'FRA', '2026-05-30 09:30:00', '2026-05-30 16:15:00',
   380.00, 130, @airlineIdRet, 1, 'MS-704',
   'Economy', 'USD', '6h 45m', 405, 'Active');

-- Return Flight 3 – Economy  |  $250  |  13:00 → 19:30  (6h 30m)
INSERT INTO airline_Flights
  (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime,
   Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber,
   FlightClass, Currency, Duration, DurationMinutes, Status)
VALUES
  ('ASW', 'FRA', '2026-05-30 13:00:00', '2026-05-30 19:30:00',
   250.00, 190, @airlineIdRet, 0, 'MS-706',
   'Economy', 'USD', '6h 30m', 390, 'Active');

-- Return Flight 4 – Business  |  $820  |  07:00 → 13:30  (6h 30m)
INSERT INTO airline_Flights
  (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime,
   Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber,
   FlightClass, Currency, Duration, DurationMinutes, Status)
VALUES
  ('ASW', 'FRA', '2026-05-30 07:00:00', '2026-05-30 13:30:00',
   820.00, 45, @airlineIdRet, 0, 'MS-708',
   'Business', 'USD', '6h 30m', 390, 'Active');

-- Return Flight 5 – First Class  |  $1,450  |  10:00 → 16:30  (6h 30m)
INSERT INTO airline_Flights
  (DepartureAirportCode, ArrivalAirportCode, DepartureTime, ArrivalTime,
   Price, AvailableSeats, AirlineId, NumberOfStops, FlightNumber,
   FlightClass, Currency, Duration, DurationMinutes, Status)
VALUES
  ('ASW', 'FRA', '2026-05-30 10:00:00', '2026-05-30 16:30:00',
   1450.00, 18, @airlineIdRet, 0, 'MS-710',
   'First', 'USD', '6h 30m', 390, 'Active');

-- ✅ Verify Return flights:
SELECT f.Id, f.FlightNumber, f.FlightClass, f.Price,
       f.DepartureTime, f.ArrivalTime, f.Duration,
       f.NumberOfStops, f.AvailableSeats
FROM airline_Flights f
WHERE f.DepartureAirportCode = 'ASW'
  AND f.ArrivalAirportCode   = 'FRA'
  AND CAST(f.DepartureTime AS DATE) = '2026-05-30'
ORDER BY f.Price;
