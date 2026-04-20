using UserAuthorizationandAuthentication.TourGuide.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserAuthorizationandAuthentication.TourGuide.DTOs.WithdrawRequest;
using UserAuthorizationandAuthentication.Models;
using UserAuthorizationandAuthentication.Models.Enums;
using UserAuthorizationandAuthentication.TourGuide.Models.Enums;

namespace UserAuthorizationandAuthentication.TourGuide.Services
{
    public class WithdrawRequestService : IWithdrawRequestService
    {
        private readonly ApplicationDbContext _context;

        public WithdrawRequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<WithdrawRequestResponseDto> CreateWithdrawRequestAsync(long tourGuideId, CreateWithdrawRequestDto model)
        {
            var guide = await _context.TourGuides.FindAsync(tourGuideId);
            if (guide == null)
                throw new Exception("Tour Guide not found.");

            var request = new WithdrawRequest
            {
                TourGuideId = tourGuideId,
                Amount = model.Amount,
                Status = WithdrawRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.WithdrawRequests.Add(request);
            await _context.SaveChangesAsync();

            return await MapToDto(request);
        }

        public async Task<List<WithdrawRequestResponseDto>> GetAllPendingRequestsAsync()
        {
            var requests = await _context.WithdrawRequests
                .Include(r => r.TourGuide)
                .Where(r => r.Status == WithdrawRequestStatus.Pending)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            var result = new List<WithdrawRequestResponseDto>();
            foreach (var r in requests)
            {
                result.Add(await MapToDto(r));
            }
            return result;
        }

        public async Task<List<WithdrawRequestResponseDto>> GetMyRequestsAsync(long tourGuideId)
        {
            var requests = await _context.WithdrawRequests
                .Include(r => r.TourGuide)
                .Where(r => r.TourGuideId == tourGuideId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var result = new List<WithdrawRequestResponseDto>();
            foreach (var r in requests)
            {
                result.Add(await MapToDto(r));
            }
            return result;
        }

        public async Task<WithdrawRequestResponseDto> ProcessRequestAsync(long requestId, ProcessWithdrawRequestDto model)
        {
            var request = await _context.WithdrawRequests
                .Include(r => r.TourGuide)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                throw new Exception("Withdraw request not found.");
            }

            if (Enum.TryParse<WithdrawRequestStatus>(model.Status, true, out var newStatus))
            {
                request.Status = newStatus;
            }
            else
            {
                throw new Exception("Invalid status provided.");
            }
            
            request.AdminNotes = model.AdminNotes;
            request.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await MapToDto(request);
        }

        private async Task<WithdrawRequestResponseDto> MapToDto(WithdrawRequest request)
        {
            if (request.TourGuide == null)
            {
                await _context.Entry(request).Reference(r => r.TourGuide).LoadAsync();
            }

            return new WithdrawRequestResponseDto
            {
                Id = request.Id,
                TourGuideId = request.TourGuideId,
                TourGuideName = request.TourGuide?.Name ?? "Unknown",
                Amount = request.Amount,
                Status = request.Status.ToString(),
                CreatedAt = request.CreatedAt,
                ProcessedAt = request.ProcessedAt,
                AdminNotes = request.AdminNotes
            };
        }
    }
}


