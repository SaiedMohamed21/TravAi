using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TravAi.Models.Admin;
using TravAi.Models.Admin.DTOs;
using TravAi.Services.Admin.Payout;

namespace TravAi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/payouts")]
    [Authorize(Roles = "Admin")]
    public class AdminPayoutController : ControllerBase
    {
        private readonly IAdminPayoutService _payoutService;

        public AdminPayoutController(IAdminPayoutService payoutService)
        {
            _payoutService = payoutService;
        }

        private long GetAdminUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !long.TryParse(userIdString, out long userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token.");
            }
            return userId;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetPayouts(
            [FromQuery] ProviderType? providerType,
            [FromQuery] string? status,
            [FromQuery] DateTime? weekStart,
            [FromQuery] DateTime? weekEnd,
            [FromQuery] int? month,
            [FromQuery] int? year)
        {
            try
            {
                var batches = await _payoutService.GetPayoutBatchesAsync(providerType, status, weekStart, weekEnd, month, year);
                return Ok(new { success = true, data = batches });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("generate-weekly")]
        public async Task<IActionResult> GenerateWeekly([FromBody] GenerateWeeklyPayoutsRequestDto request)
        {
            try
            {
                long adminId = GetAdminUserId();
                var generated = await _payoutService.GenerateWeeklyPayoutsAsync(adminId, request);
                return Ok(new { success = true, data = generated, message = $"Generated {generated.Count} missing weekly invoices." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayoutDetails(long id)
        {
            try
            {
                var details = await _payoutService.GetPayoutBatchDetailsAsync(id);
                if (details == null) return NotFound(new { success = false, message = "Payout batch not found" });
                
                return Ok(new { success = true, data = details });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id}/confirm")]
        public async Task<IActionResult> ConfirmPayout(long id, [FromBody] ConfirmPayoutRequestDto request)
        {
            try
            {
                long adminId = GetAdminUserId();
                var confirmed = await _payoutService.ConfirmPayoutAsync(id, adminId, request);
                return Ok(new { success = true, data = confirmed, message = "Payout confirmed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id}/mark-failed")]
        public async Task<IActionResult> MarkPayoutFailed(long id, [FromBody] MarkPayoutFailedRequestDto request)
        {
            try
            {
                long adminId = GetAdminUserId();
                var failed = await _payoutService.MarkPayoutFailedAsync(id, adminId, request);
                return Ok(new { success = true, data = failed, message = "Payout marked as failed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetPayoutSummary()
        {
            try
            {
                var summary = await _payoutService.GetPayoutSummaryAsync();
                return Ok(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id}/create-payment-session")]
        public async Task<IActionResult> CreatePaymentSession(long id)
        {
            try
            {
                long adminId = GetAdminUserId();
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var successUrl = $"{baseUrl}/admin-payout/admin-payout-details.html?id={id}&payment=success&session_id={{CHECKOUT_SESSION_ID}}";
                var cancelUrl = $"{baseUrl}/admin-payout/admin-payout-details.html?id={id}&payment=cancelled";

                var result = await _payoutService.CreatePayoutStripeSessionAsync(id, adminId, successUrl, cancelUrl);
                
                if (result.skipped)
                {
                    return Ok(new { success = true, skipped = true, message = result.message });
                }

                return Ok(new { success = true, checkoutUrl = result.checkoutUrl, message = result.message });
            }
            catch (StripeDestinationException ex)
            {
                return BadRequest(new 
                { 
                    success = false, 
                    message = ex.Message,
                    payoutId = ex.PayoutId,
                    providerType = ex.ProviderType,
                    providerId = ex.ProviderId,
                    destinationAccount = ex.DestinationAccount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{id}/verify-payment")]
        public async Task<IActionResult> VerifyPayment(long id, [FromQuery] string session_id)
        {
            try
            {
                long adminId = GetAdminUserId();
                if (string.IsNullOrEmpty(session_id)) return BadRequest(new { success = false, message = "session_id is required" });

                var result = await _payoutService.VerifyPayoutStripePaymentAsync(id, session_id, adminId);
                return Ok(new 
                { 
                    success = true, 
                    data = result, 
                    message = "Payment verified and payout confirmed.",
                    checkoutSessionId = result.CheckoutSessionId,
                    paymentIntentId = result.PaymentIntentId,
                    paymentIntentDestination = result.PaymentIntentDestination,
                    transferId = result.TransferId,
                    providerAccountFromDb = result.ProviderAccountFromDb
                });
            }
            catch (StripeDestinationVerificationException ex)
            {
                return BadRequest(new 
                { 
                    success = false, 
                    message = ex.Message,
                    providerAccountFromDb = ex.ProviderAccountFromDb,
                    paymentIntentDestination = ex.PaymentIntentDestination,
                    paymentIntentId = ex.PaymentIntentId,
                    checkoutSessionId = ex.CheckoutSessionId,
                    transferId = ex.TransferId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}/stripe-payment")]
        public async Task<IActionResult> GetStripePayment(long id)
        {
            try
            {
                var payment = await _payoutService.GetPayoutStripePaymentAsync(id);
                if (payment == null) return NotFound(new { success = false, message = "No completed or skipped payment record found." });

                return Ok(new { success = true, data = payment });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("stripe-connect-diagnostics")]
        public async Task<IActionResult> StripeConnectDiagnostics([FromQuery] string destination)
        {
            var (canSee, availableAccounts, errorMessage) = await _payoutService.TestStripeConnectVisibilityAsync(destination);
            return Ok(new { TargetAccount = destination, CanSee = canSee, AvailableAccounts = availableAccounts, Error = errorMessage });
        }
    }
}
