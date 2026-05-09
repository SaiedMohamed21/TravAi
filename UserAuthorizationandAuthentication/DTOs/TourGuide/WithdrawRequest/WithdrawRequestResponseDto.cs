using TravAi.TourGuide.Models;
using System;

namespace TravAi.TourGuide.DTOs.WithdrawRequest
{
    public class WithdrawRequestResponseDto
    {
        public long Id { get; set; }
        public long TourGuideId { get; set; }
        public string TourGuideName { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? AdminNotes { get; set; }
    }
}



