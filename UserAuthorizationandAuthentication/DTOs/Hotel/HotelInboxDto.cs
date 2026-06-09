using System;
using System.Collections.Generic;
using TravAi.Models.Enums;

namespace TravAi.DTOs.Hotel
{
    public class HotelInboxSummaryDto
    {
        public List<HotelInboxItemDto> FinancialTransactions { get; set; } = new();
        public List<HotelInboxItemDto> WarningsAlerts { get; set; } = new();
        public List<HotelInboxItemDto> UpcomingActions { get; set; } = new();
    }

    public class HotelInboxItemDto
    {
        public long Id { get; set; }
        public InboxCategory Category { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public InboxSeverity? Severity { get; set; }
        public string? SeverityLabel { get; set; }
        public string? SeverityColor { get; set; }
        
        public string? RefType { get; set; }
        public string? RefId { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? ResolutionDate { get; set; }
        public string? ActionLabel { get; set; }
        public InboxPriority? Priority { get; set; }
        public string? PriorityLabel { get; set; }

        public InboxStatus Status { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<HotelInboxReplyDto> Replies { get; set; } = new();
    }

    public class HotelInboxReplyDto
    {
        public long Id { get; set; }
        public string Message { get; set; }
        public string FromName { get; set; }
        public bool IsFromAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class HotelInboxReplyRequest
    {
        public string Message { get; set; }
    }

    public class HotelInboxComposeRequest
    {
        public string Subject { get; set; }
        public string Message { get; set; }
        public HotelToAdminCategory Category { get; set; }
    }

    public class HotelInboxPagedDto<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
