using TravAi;
using TravAi.Data;
using TravAi.Airline.DTOs.Dashboard;

namespace TravAi.Airline.Services.DashboardService
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync();
    }
}



