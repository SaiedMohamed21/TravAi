using TravAi.TourGuide.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace TravAi.TourGuide.DTOs.WithdrawRequest
{
    public class ProcessWithdrawRequestDto
    {
        [Required]
        public string Status { get; set; } // Approved or Rejected
        
        public string? AdminNotes { get; set; }
    }
}



