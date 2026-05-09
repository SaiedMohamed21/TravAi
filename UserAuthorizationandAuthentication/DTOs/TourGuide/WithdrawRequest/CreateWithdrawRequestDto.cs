using TravAi.TourGuide.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace TravAi.TourGuide.DTOs.WithdrawRequest
{
    public class CreateWithdrawRequestDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
    }
}



