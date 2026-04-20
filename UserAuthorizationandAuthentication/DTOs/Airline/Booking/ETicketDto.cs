using System;
using System.Collections.Generic;

namespace UserAuthorizationandAuthentication.Airline.DTOs.Booking
{
    public class ETicketDto
    {
        public long BookingId { get; set; }
        public string Pnr { get; set; } = null!; // Simulated Booking Reference
        public string AirlineName { get; set; } = null!;
        public string DepartureAirport { get; set; } = null!;
        public string ArrivalAirport { get; set; } = null!;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public DateTime BookingDate { get; set; }

        public List<PassengerTicketDto> Passengers { get; set; } = new List<PassengerTicketDto>();
    }

    public class PassengerTicketDto
    {
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!; // Adult, Child, etc.
        public string QrCodeBase64 { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}


