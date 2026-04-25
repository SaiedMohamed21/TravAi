using UserAuthorizationandAuthentication.TourGuide.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace UserAuthorizationandAuthentication.TourGuide.DTOs.WithdrawRequest
{
    public class ProcessWithdrawRequestDto
    {
        [Required]
        public string Status { get; set; } // Approved or Rejected
        
        public string? AdminNotes { get; set; }
    }
}



