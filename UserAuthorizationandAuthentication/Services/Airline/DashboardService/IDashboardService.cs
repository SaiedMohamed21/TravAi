using UserAuthorizationandAuthentication;
using UserAuthorizationandAuthentication.Data;
using UserAuthorizationandAuthentication.Airline.DTOs.Dashboard;

namespace UserAuthorizationandAuthentication.Airline.Services.DashboardService
{
    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync();
    }
}



