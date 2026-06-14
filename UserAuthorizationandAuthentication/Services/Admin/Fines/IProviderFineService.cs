using System.Collections.Generic;
using System.Threading.Tasks;
using TravAi.DTOs.Admin.Fines;
using TravAi.Models.Admin;

namespace TravAi.Services.Admin.Fines
{
    public interface IProviderFineService
    {
        Task<List<ProviderFineListItemDto>> GetFinesAsync(ProviderFineFilterDto filter);
        Task<ProviderFineDetailsDto?> GetFineDetailsAsync(long id);
        Task<ProviderFineDetailsDto> CreateFineAsync(CreateProviderFineDto dto, long adminUserId);
        Task<ProviderFineDetailsDto> UpdateFineAsync(long id, UpdateProviderFineDto dto, long adminUserId);
        Task CancelFineAsync(long id, CancelProviderFineDto dto, long adminUserId);
        Task<List<EligibleFineComplaintDto>> GetEligibleComplaintsAsync(ProviderType type, string? search);
        Task<List<EligibleTourCancellationDto>> GetTourGuideCancelledBookingsAsync(string? search);
        Task<List<ProviderLookupDto>> GetProvidersLookupAsync(ProviderType type, string? search);
        Task<List<ProviderFineListItemDto>> GetFinesByComplaintIdAsync(long complaintId);
        Task<EligibleTourCancellationDto?> GetTourCancellationDetailsAsync(long tourBookingId);
    }
}
