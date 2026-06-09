using System;
using System.Collections.Generic;
using TravAi.Models.Enums;

namespace TravAi.DTOs.AdminManagement
{
    public class AmenityManageDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public bool IsHighlighted { get; set; }
    }

    public class DocumentTypeManageDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string KeyName { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsActive { get; set; }
    }

    public class FieldDefinitionManageDto
    {
        public long Id { get; set; }
        public string KeyName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public HotelFieldType FieldType { get; set; }
        public bool IsRequired { get; set; }
        public bool IsActive { get; set; }
    }

    public class HotelManageSummaryDto
    {
        public long HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public VerificationStatus Status { get; set; }
        public bool IsActive { get; set; }

        public bool CanApprove { get; set; }
        public bool CanSuspend { get; set; }
        public bool CanBan { get; set; }
    }
}
