using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserAuthorizationandAuthentication.Data;

using System.Security.Claims;
using UserAuthorizationandAuthentication.DTOs.Common;
using UserAuthorizationandAuthentication.DTOs.Auth;
using UserAuthorizationandAuthentication.Airline.DTOs.Wallet;
using UserAuthorizationandAuthentication.Airline.Models;

namespace UserAuthorizationandAuthentication.Airline.Controllers
{
    [ApiController]
    [Route("api/airline/wallet")]
    [Authorize]
    [ApiExplorerSettings(GroupName = "Airline")]
    public class WalletController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WalletController(ApplicationDbContext context)
        {
            _context = context;
        }

        private long GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated");
            }
            return userId;
        }

        // GET: api/airline/wallet/balance
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            long userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new ApiResponse<object>(null, "User not found"));

            return Ok(new ApiResponse<object>(new { Balance = user.WalletBalance }, "Balance retrieved successfully"));
        }

        // GET: api/airline/wallet/transactions
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions()
        {
            long userId = GetCurrentUserId();
            var transactions = await _context.WalletTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new WalletTransactionDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Type = t.Type,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    ReferenceId = t.ReferenceId
                })
                .ToListAsync();

            return Ok(new ApiResponse<object>(transactions, "Transactions retrieved successfully"));
        }

        // POST: api/airline/wallet/add-funds
        [HttpPost("add-funds")]
        public async Task<IActionResult> AddFunds([FromBody] AddFundsDto dto)
        {
            if (dto.Amount <= 0)
            {
                return BadRequest(new ApiResponse<object>(null, "Amount must be greater than zero"));
            }

            long userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new ApiResponse<object>(null, "User not found"));

            // Start transaction since we'll update two tables
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                user.WalletBalance += dto.Amount;

                var walletTx = new UserAuthorizationandAuthentication.Airline.Models.WalletTransaction
                {
                    UserId = userId,
                    Amount = dto.Amount,
                    Type = "Deposit",
                    Description = string.IsNullOrWhiteSpace(dto.Description) ? "Wallet Top-up" : dto.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _context.WalletTransactions.Add(walletTx);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new ApiResponse<object>(
                    new { Balance = user.WalletBalance, Transaction = walletTx }, 
                    "Funds added successfully"));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new ApiResponse<object>(null, "An error occurred while adding funds"));
            }
        }
    }
}
