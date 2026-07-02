using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TeamTrack.DTOs;
using TeamTrack.Services;

namespace TeamTrack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ProductManager")]
    public class JiraImportController : ControllerBase
    {
        private readonly IJiraImportService _importService;

        public JiraImportController(IJiraImportService importService)
        {
            _importService = importService;
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<string>.FailureResponse("No file uploaded."));
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<string>.FailureResponse("Invalid file format. Please upload a CSV file."));
            }

            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                using (var stream = file.OpenReadStream())
                {
                    var result = await _importService.ImportJiraCsvAsync(stream, userId);

                    if (result.Errors.Count > 0)
                    {
                        return BadRequest(ApiResponse<JiraImportResponseDto>.FailureResponse(
                            string.Join("; ", result.Errors)));
                    }

                    return Ok(ApiResponse<JiraImportResponseDto>.SuccessResponse(
                        result, 
                        $"Import completed successfully. Imported: {result.ProjectsImported} Projects, {result.UsersImported} Users, {result.WorkItemsImported} Work Items, {result.BugsImported} Bugs."));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<string>.FailureResponse($"An unexpected error occurred during import: {ex.Message}"));
            }
        }
    }
}
