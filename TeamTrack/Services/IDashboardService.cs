using TeamTrack.DTOs;

namespace TeamTrack.Services
{
    public interface IDashboardService
    {
        Task<DashboardResponseDto> GetDashboardAsync();
    }
}