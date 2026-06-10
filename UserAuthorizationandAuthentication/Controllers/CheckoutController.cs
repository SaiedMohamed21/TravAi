using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TravAi.DTOs.Checkout;
using TravAi.DTOs.Common;
using TravAi.Services;

namespace TravAi.Controllers
{
    [ApiController]
    [Route("api/checkout")]
    public class CheckoutController : ControllerBase
    {
        private readonly ICheckoutService _checkoutService;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(ICheckoutService checkoutService, ILogger<CheckoutController> logger)
        {
            _checkoutService = checkoutService;
            _logger = logger;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending([FromQuery] long userId)
        {
            try
            {
                var items = await _checkoutService.GetPendingBookingsAsync(userId);
                return Ok(new ApiResponse<object>(items, "Pending bookings retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending bookings for user {UserId}", userId);
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
        }

        [HttpPost("airline")]
        public async Task<IActionResult> CreateAirlineCheckout([FromBody] CreateAirlineCheckoutRequest request)
        {
            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var response = await _checkoutService.CreateAirlineCheckoutAsync(request, baseUrl);
                return Ok(new ApiResponse<CheckoutResponse>(response, "Airline checkout session created successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating airline checkout for user {UserId}", request.UserId);
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
        }

        [HttpPost("hotel-tour")]
        public async Task<IActionResult> CreateHotelTourCheckout([FromBody] CreateHotelTourCheckoutRequest request)
        {
            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var response = await _checkoutService.CreateHotelTourCheckoutAsync(request, baseUrl);
                return Ok(new ApiResponse<CheckoutResponse>(response, "Hotel and Tour checkout session created successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating hotel/tour checkout for user {UserId}", request.UserId);
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
        }

        [HttpGet("session/{id}")]
        public async Task<IActionResult> GetSessionDetails(long id)
        {
            try
            {
                var details = await _checkoutService.GetCheckoutSessionDetailsAsync(id);
                if (details == null)
                    return NotFound(new ApiResponse<string>(false, "Checkout session not found."));

                return Ok(new ApiResponse<object>(details, "Session details retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting checkout session details for id {Id}", id);
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
        }

        [HttpGet("confirm")]
        public async Task<IActionResult> ConfirmPayment([FromQuery] string session_id)
        {
            try
            {
                var result = await _checkoutService.ConfirmPaymentAsync(session_id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment for Stripe session {SessionId}", session_id);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("stripe/webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                var stripeSignature = Request.Headers["Stripe-Signature"];
                var processed = await _checkoutService.HandleStripeWebhookAsync(json, stripeSignature);
                return Ok(new { received = true, processed });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe webhook");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
