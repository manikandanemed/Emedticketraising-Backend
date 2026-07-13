using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamTrack.DTOs;
using TeamTrack.Models;
using TeamTrack.Repositories;

namespace TeamTrack.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TicketController : ControllerBase
    {
        private readonly IRepository<Ticket> _ticketRepo;
        private readonly IRepository<SoftwareBuild> _buildRepo;
        private readonly IRepository<Project> _projectRepo;

        public TicketController(
            IRepository<Ticket> ticketRepo,
            IRepository<SoftwareBuild> buildRepo,
            IRepository<Project> projectRepo)
        {
            _ticketRepo = ticketRepo;
            _buildRepo = buildRepo;
            _projectRepo = projectRepo;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(ApiResponse<string>.FailureResponse("Ticket title is required."));

            var projectExists = await _projectRepo.Query().AnyAsync(p => p.Id == request.ProjectId);
            if (!projectExists)
                return NotFound(ApiResponse<string>.FailureResponse("Project not found."));

            if (request.BuildId.HasValue)
            {
                var build = await _buildRepo.GetAsync(b => b.Id == request.BuildId.Value && b.IsActive);
                if (build == null)
                    return NotFound(ApiResponse<string>.FailureResponse("Selected Build not found or inactive."));

                if (build.ProjectId != request.ProjectId)
                    return BadRequest(ApiResponse<string>.FailureResponse("Selected Build does not belong to the same Project as the Ticket."));
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var count = await _ticketRepo.Query().CountAsync();

            var ticket = new Ticket
            {
                TicketNumber = $"TCK-{(count + 1):D3}",
                Title = request.Title.Trim(),
                Description = request.Description,
                Category = request.Category,
                Priority = request.Priority,
                Status = "open",
                ProjectId = request.ProjectId,
                RaisedByUserId = userId,
                AssignedToUserId = request.AssignedToUserId,
                BuildId = request.BuildId,
                WhatsappNotify = request.WhatsappNotify,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _ticketRepo.AddAsync(ticket);
            await _ticketRepo.SaveAsync();

            var responseDto = await GetTicketResponseDto(ticket.Id);
            return Ok(ApiResponse<TicketResponseDto>.SuccessResponse(responseDto!, "Ticket created successfully."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTicket(int id, [FromBody] UpdateTicketDto request)
        {
            var ticket = await _ticketRepo.GetAsync(t => t.Id == id);
            if (ticket == null)
                return NotFound(ApiResponse<string>.FailureResponse("Ticket not found."));

            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(ApiResponse<string>.FailureResponse("Ticket title is required."));

            if (request.BuildId.HasValue)
            {
                var build = await _buildRepo.GetAsync(b => b.Id == request.BuildId.Value && b.IsActive);
                if (build == null)
                    return NotFound(ApiResponse<string>.FailureResponse("Selected Build not found or inactive."));

                if (build.ProjectId != ticket.ProjectId)
                    return BadRequest(ApiResponse<string>.FailureResponse("Selected Build does not belong to the same Project as the Ticket."));
            }

            ticket.Title = request.Title.Trim();
            ticket.Description = request.Description;
            ticket.Category = request.Category;
            ticket.Priority = request.Priority;
            ticket.Status = request.Status;
            ticket.AssignedToUserId = request.AssignedToUserId;
            ticket.BuildId = request.BuildId;
            ticket.WhatsappNotify = request.WhatsappNotify;
            ticket.UpdatedAt = DateTime.UtcNow;

            if (request.Status.ToLower() == "resolved" && !ticket.ResolvedAt.HasValue)
            {
                ticket.ResolvedAt = DateTime.UtcNow;
            }
            else if (request.Status.ToLower() != "resolved")
            {
                ticket.ResolvedAt = null;
            }

            if (request.Status.ToLower() == "closed" && !ticket.ClosedAt.HasValue)
            {
                ticket.ClosedAt = DateTime.UtcNow;
            }
            else if (request.Status.ToLower() != "closed")
            {
                ticket.ClosedAt = null;
            }

            await _ticketRepo.SaveAsync();

            var responseDto = await GetTicketResponseDto(ticket.Id);
            return Ok(ApiResponse<TicketResponseDto>.SuccessResponse(responseDto!, "Ticket updated successfully."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(int id)
        {
            var responseDto = await GetTicketResponseDto(id);
            if (responseDto == null)
                return NotFound(ApiResponse<string>.FailureResponse("Ticket not found."));

            return Ok(ApiResponse<TicketResponseDto>.SuccessResponse(responseDto, "Ticket details fetched successfully."));
        }

        [HttpGet("build/{buildId}")]
        public async Task<IActionResult> GetTicketsByBuild(int buildId)
        {
            var tickets = await _ticketRepo.Query()
                .Include(t => t.Project)
                .Include(t => t.RaisedBy)
                .Include(t => t.AssignedTo)
                .Include(t => t.Build)
                .Where(t => t.BuildId == buildId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TicketResponseDto
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    Title = t.Title,
                    Description = t.Description,
                    Category = t.Category,
                    Priority = t.Priority,
                    Status = t.Status,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project != null ? t.Project.Name : string.Empty,
                    RaisedByUserId = t.RaisedByUserId,
                    RaisedByUserName = t.RaisedBy != null ? t.RaisedBy.Name : string.Empty,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedToUserName = t.AssignedTo != null ? t.AssignedTo.Name : null,
                    BuildId = t.BuildId,
                    BuildNumber = t.Build != null ? t.Build.BuildNumber : null,
                    WhatsappNotify = t.WhatsappNotify,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    ResolvedAt = t.ResolvedAt,
                    ClosedAt = t.ClosedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<List<TicketResponseDto>>.SuccessResponse(tickets, "Tickets for the build fetched successfully."));
        }

        private async Task<TicketResponseDto?> GetTicketResponseDto(int ticketId)
        {
            return await _ticketRepo.Query()
                .Include(t => t.Project)
                .Include(t => t.RaisedBy)
                .Include(t => t.AssignedTo)
                .Include(t => t.Build)
                .Where(t => t.Id == ticketId)
                .Select(t => new TicketResponseDto
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,
                    Title = t.Title,
                    Description = t.Description,
                    Category = t.Category,
                    Priority = t.Priority,
                    Status = t.Status,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project != null ? t.Project.Name : string.Empty,
                    RaisedByUserId = t.RaisedByUserId,
                    RaisedByUserName = t.RaisedBy != null ? t.RaisedBy.Name : string.Empty,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedToUserName = t.AssignedTo != null ? t.AssignedTo.Name : null,
                    BuildId = t.BuildId,
                    BuildNumber = t.Build != null ? t.Build.BuildNumber : null,
                    WhatsappNotify = t.WhatsappNotify,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    ResolvedAt = t.ResolvedAt,
                    ClosedAt = t.ClosedAt
                })
                .FirstOrDefaultAsync();
        }
    }
}
