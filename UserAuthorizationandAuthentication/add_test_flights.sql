-- Add 3 Flights for testing cancellation with different classes
-- Date: 2027/12/31

DECLARE @AirlineId BIGINT = 1; -- Using a default AirlineId (ensure this exists in airline_Airlines)

-- 1. Economy Flight
INSERT INTO [airline_Flights] (
    [AirlineId], [FlightNumber], [DepartureAirportCode], [ArrivalAirportCode], 
    [DepartureTime], [ArrivalTime], [BasePrice], [TotalSeats], [AvailableSeats], 
    [Status], [FlightClass], [CreatedAt], [UpdatedAt]
)
VALUES (
    @AirlineId, 'ECO-2027', 'CAI', 'DXB', 
    '2027-12-31 10:00:00', '2027-12-31 14:00:00', 350.00, 150, 150, 
    'Scheduled', 'Economy', GETUTCDATE(), GETUTCDATE()
);

-- 2. Premium Flight
INSERT INTO [airline_Flights] (
    [AirlineId], [FlightNumber], [DepartureAirportCode], [ArrivalAirportCode], 
    [DepartureTime], [ArrivalTime], [BasePrice], [TotalSeats], [AvailableSeats], 
    [Status], [FlightClass], [CreatedAt], [UpdatedAt]
)
VALUES (
    @AirlineId, 'PRM-2027', 'CAI', 'LHR', 
    '2027-12-31 11:30:00', '2027-12-31 16:30:00', 700.00, 50, 50, 
    'Scheduled', 'Premium Economy', GETUTCDATE(), GETUTCDATE()
);

-- 3. Business Flight
INSERT INTO [airline_Flights] (
    [AirlineId], [FlightNumber], [DepartureAirportCode], [ArrivalAirportCode], 
    [DepartureTime], [ArrivalTime], [BasePrice], [TotalSeats], [AvailableSeats], 
    [Status], [FlightClass], [CreatedAt], [UpdatedAt]
)
VALUES (
    @AirlineId, 'BUS-2027', 'CAI', 'JFK', 
    '2027-12-31 13:00:00', '2027-12-31 22:00:00', 2500.00, 30, 30, 
    'Scheduled', 'Business', GETUTCDATE(), GETUTCDATE()
);

PRINT 'Successfully added 3 test flights (Economy, Premium Economy, Business) for 2027-12-31.';
