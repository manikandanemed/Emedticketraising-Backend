using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTrack.Data;
using TeamTrack.Models;
using TeamTrack.DTOs;
using Microsoft.EntityFrameworkCore;

namespace TeamTrack.Controllers
{
    [ApiController]
    [Route("api/employees")]
    [Authorize(Roles = "ProductManager")]
    public class EmployeeNoteController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmployeeNoteController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("{employeeId}/notes")]
        public async Task<IActionResult> AddNote(int employeeId, [FromBody] AddNoteRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NoteText))
            {
                return BadRequest(ApiResponse<object>.FailureResponse("Note text cannot be empty."));
            }

            // Verify employee exists
            var employeeExists = await _context.Users.AnyAsync(u => u.Id == employeeId && u.UserType == "Employee");
            if (!employeeExists)
            {
                return NotFound(ApiResponse<object>.FailureResponse("Employee not found."));
            }

            // Get current PM ID from JWT Claims
            var pmIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (pmIdClaim == null)
            {
                return Unauthorized(ApiResponse<object>.FailureResponse("Unauthorized."));
            }
            int pmId = int.Parse(pmIdClaim.Value);

            var note = new DailyStatusNote
            {
                EmployeeId = employeeId,
                CreatedByUserId = pmId,
                NoteText = request.NoteText,
                CreatedAt = DateTime.UtcNow
            };

            _context.DailyStatusNotes.Add(note);
            await _context.SaveChangesAsync();

            // Fetch PM name for the response DTO
            var pmName = User.Identity?.Name ?? "Product Manager";

            var dto = new DailyStatusNoteDto
            {
                Id = note.Id,
                NoteText = note.NoteText,
                CreatedAt = note.CreatedAt,
                CreatedByName = pmName
            };

            return Ok(ApiResponse<DailyStatusNoteDto>.SuccessResponse(dto, "Daily status note added successfully."));
        }
    }

    public class AddNoteRequest
    {
        public string NoteText { get; set; } = string.Empty;
    }
}
