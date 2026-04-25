using UserAuthorizationandAuthentication.TourGuide.Models;
using System;

namespace UserAuthorizationandAuthentication.TourGuide.DTOs.UrgentRequest
{
    public class UrgentRequestResponseDto
    {
        public long Id { get; set; }
        public long TourGuideId { get; set; }
        public string TourGuideName { get; set; }
        public long TourId { get; set; }
        public string TourTitle { get; set; }
        public string Reason { get; set; }
        public string? DocumentationUrl { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? AdminNotes { get; set; }
    }
}



