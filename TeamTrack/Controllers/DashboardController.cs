using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTrack.DTOs;
using TeamTrack.Services;

namespace TeamTrack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ProductManager")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var result = await _dashboardService.GetDashboardAsync();
            return Ok(ApiResponse<DashboardResponseDto>.SuccessResponse(result, "Dashboard fetched successfully"));
        }
    }
}