using System;
using System.Collections.Generic;
using TravAi.Models.Enums;

namespace TravAi.DTOs.AdminManagement
{
    public class AdminBookingSearchRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? BookingId { get; set; }
        public string? Hotel { get; set; }
        public string? City { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public BookingStatus? Status { get; set; }
    }

    public class AdminBookingPaginationResponse
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<AdminBookingItemDto> Items { get; set; } = new();
    }

    public class AdminBookingItemDto
    {
        public long BookingId { get; set; }
        public string User { get; set; } = string.Empty;
        public string Hotel { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string CheckIn { get; set; } = string.Empty;
        public string CheckOut { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
