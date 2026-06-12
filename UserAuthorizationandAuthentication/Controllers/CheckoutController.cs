using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
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
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    if (long.TryParse(userIdStr, out long parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                    else
                    {
                        return Unauthorized(new ApiResponse<string>(false, "Invalid user ID in token."));
                    }
                }
                else if (Request.Headers.ContainsKey("Authorization"))
                {
                    return Unauthorized(new ApiResponse<string>(false, "Invalid or expired authorization token."));
                }

                var items = await _checkoutService.GetPendingBookingsAsync(userId);
                return Ok(new ApiResponse<object>(items, "Pending bookings retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending bookings for user {UserId}", userId);
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateCheckout([FromBody] CreateUnifiedCheckoutRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    if (long.TryParse(userIdStr, out long parsedUserId))
                    {
                        request.UserId = parsedUserId;
                    }
                    else
                    {
                        return Unauthorized(new ApiResponse<string>(false, "Invalid user ID in token."));
                    }
                }
                else if (Request.Headers.ContainsKey("Authorization"))
                {
                    return Unauthorized(new ApiResponse<string>(false, "Invalid or expired authorization token."));
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var response = await _checkoutService.CreateUnifiedCheckoutAsync(request, baseUrl);
                return Ok(new ApiResponse<CheckoutResponse>(response, "Checkout session created successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<string>(false, ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<string>(false, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout for user {UserId}", request?.UserId);
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
        }

        [HttpPost("airline")]
        public async Task<IActionResult> CreateAirlineCheckout([FromBody] CreateAirlineCheckoutRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    if (long.TryParse(userIdStr, out long parsedUserId))
                    {
                        request.UserId = parsedUserId;
                    }
                    else
                    {
                        return Unauthorized(new ApiResponse<string>(false, "Invalid user ID in token."));
                    }
                }
                else if (Request.Headers.ContainsKey("Authorization"))
                {
                    return Unauthorized(new ApiResponse<string>(false, "Invalid or expired authorization token."));
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var response = await _checkoutService.CreateAirlineCheckoutAsync(request, baseUrl);
                return Ok(new ApiResponse<CheckoutResponse>(response, "Airline checkout session created successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<string>(false, ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<string>(false, ex.Message));
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
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    if (long.TryParse(userIdStr, out long parsedUserId))
                    {
                        request.UserId = parsedUserId;
                    }
                    else
                    {
                        return Unauthorized(new ApiResponse<string>(false, "Invalid user ID in token."));
                    }
                }
                else if (Request.Headers.ContainsKey("Authorization"))
                {
                    return Unauthorized(new ApiResponse<string>(false, "Invalid or expired authorization token."));
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var response = await _checkoutService.CreateHotelTourCheckoutAsync(request, baseUrl);
                return Ok(new ApiResponse<CheckoutResponse>(response, "Hotel and Tour checkout session created successfully."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<string>(false, ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<string>(false, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating hotel/tour checkout for user {UserId}", request.UserId);
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
        }

        [HttpPost("wallet-pay")]
        public async Task<IActionResult> PayWithWallet([FromBody] CreateUnifiedCheckoutRequest request)
        {
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    if (long.TryParse(userIdStr, out long parsedUserId))
                    {
                        request.UserId = parsedUserId;
                    }
                    else
                    {
                        return Unauthorized(new ApiResponse<string>(false, "Invalid user ID in token."));
                    }
                }
                else if (Request.Headers.ContainsKey("Authorization"))
                {
                    return Unauthorized(new ApiResponse<string>(false, "Invalid or expired authorization token."));
                }

                var success = await _checkoutService.PayWithWalletAsync(request, request.UserId);
                if (success)
                {
                    return Ok(new ApiResponse<object>(new { success = true }, "Paid successfully using wallet."));
                }
                return BadRequest(new ApiResponse<string>(false, "Payment failed."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<string>(false, ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new ApiResponse<string>(false, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error paying with wallet for user {UserId}", request.UserId);
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

        [HttpGet("payments/{paymentTransactionId}")]
        public async Task<IActionResult> GetPaymentDetails(long paymentTransactionId)
        {
            try
            {
                var payment = await _checkoutService.GetPaymentTransactionDetailsAsync(paymentTransactionId);
                if (payment == null)
                    return NotFound(new ApiResponse<string>(false, "Payment transaction not found."));

                return Ok(new ApiResponse<object>(payment, "Payment transaction retrieved successfully."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment transaction details for id {Id}", paymentTransactionId);
                return BadRequest(new ApiResponse<string>(false, ex.Message));
            }
        }
    }
}
