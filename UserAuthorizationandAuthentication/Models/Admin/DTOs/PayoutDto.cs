using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TravAi.Models.Admin;

namespace TravAi.Models.Admin.DTOs
{
    public class GenerateWeeklyPayoutsRequestDto
    {
        public DateTime? FilterWeekStartDate { get; set; }
        public ProviderType? ProviderType { get; set; }
    }

    public class PayoutBatchDto
    {
        public long Id { get; set; }
        public string ProviderType { get; set; }
        public long ProviderId { get; set; }
        public string? ProviderNameSnapshot { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public string Status { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal TotalRefundAmount { get; set; }
        public decimal NetAfterRefundAmount { get; set; }
        public decimal TotalCommissionAmount { get; set; }
        public decimal TotalFineDeductionAmount { get; set; }
        public decimal FinalPayoutAmount { get; set; }
        public string Currency { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string? GeneratedByAdminUserEmail { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? ConfirmedByAdminUserEmail { get; set; }
        public DateTime? FailedAt { get; set; }
        public string? FailureReason { get; set; }
        public string? Notes { get; set; }
        
        // Stripe Diagnostic Fields
        public string? CheckoutSessionId { get; set; }
        public string? PaymentIntentId { get; set; }
        public string? PaymentIntentDestination { get; set; }
        public string? TransferId { get; set; }
        public string? ProviderAccountFromDb { get; set; }
    }

    public class PayoutBatchDetailDto : PayoutBatchDto
    {
        public List<PayoutItemDto> Items { get; set; } = new List<PayoutItemDto>();
        public List<PayoutFineDeductionDto> Deductions { get; set; } = new List<PayoutFineDeductionDto>();
    }

    public class PayoutItemDto
    {
        public long Id { get; set; }
        public string BookingType { get; set; }
        public long BookingId { get; set; }
        public string? GuestName { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public DateTime ServiceEndDate { get; set; }
        public decimal OriginalPaidAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal NetAfterRefundAmount { get; set; }
        public string? RefundReason { get; set; }
        public decimal CommissionPercentage { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal ProviderAmount { get; set; }
        public string Currency { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PayoutFineDeductionDto
    {
        public long Id { get; set; }
        public long ProviderFineId { get; set; }
        public decimal Amount { get; set; }
        public string? ReasonSnapshot { get; set; }
        public DateTime FineCreatedAt { get; set; }
        public DateTime AppliedAt { get; set; }
        public long? SourceComplaintId { get; set; }
    }

    public class ConfirmPayoutRequestDto
    {
        [MaxLength(2000)]
        public string? Notes { get; set; }
    }

    public class MarkPayoutFailedRequestDto
    {
        [Required]
        [MaxLength(1000)]
        public string FailureReason { get; set; }
    }

    public class PayoutSummaryDto
    {
        public int TotalPendingBatches { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public int TotalPaidBatches { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public int TotalFailedBatches { get; set; }
    }

    public class PayoutStripePaymentReceiptDto
    {
        public long PayoutBatchId { get; set; }
        public string ProviderType { get; set; }
        public long ProviderId { get; set; }
        public string ProviderPayoutAccountNumber { get; set; }
        public string StripeConnectedAccountId { get; set; }
        public string StripeDestinationAccount { get; set; }
        public string StripeCheckoutSessionId { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string? BankName { get; set; }
        public string? BankLast4 { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
