using Microsoft.EntityFrameworkCore;
using TeamTrack.DTOs;
using TeamTrack.Models;
using TeamTrack.Repositories;

namespace TeamTrack.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IRepository<Project> _projectRepo;
        private readonly IRepository<WorkItem> _workItemRepo;
        private readonly IRepository<Bug> _bugRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<DailyStatusNote> _noteRepo;

        public DashboardService(
            IRepository<Project> projectRepo,
            IRepository<WorkItem> workItemRepo,
            IRepository<Bug> bugRepo,
            IRepository<User> userRepo,
            IRepository<DailyStatusNote> noteRepo)
        {
            _projectRepo = projectRepo;
            _workItemRepo = workItemRepo;
            _bugRepo = bugRepo;
            _userRepo = userRepo;
            _noteRepo = noteRepo;
        }

        public async Task<DashboardResponseDto> GetDashboardAsync()
        {
            // Totals
            var totalProjects = await _projectRepo.Query().CountAsync();

            var workItems = await _workItemRepo.Query()
                .Include(w => w.Project)
                    .ThenInclude(p => p.Client)
                .Include(w => w.Module)
                    .ThenInclude(m => m.Product)
                .ToListAsync();

            var bugs = await _bugRepo.Query().ToListAsync();

            // Separate WorkItems into normal tasks and bugs based on WorkType
            var workItemBugs = workItems.Where(w => w.WorkType != null && w.WorkType.Equals("Bug", System.StringComparison.OrdinalIgnoreCase)).ToList();
            var normalWorkItems = workItems.Where(w => w.WorkType == null || !w.WorkType.Equals("Bug", System.StringComparison.OrdinalIgnoreCase)).ToList();

            var totalWorkItems = normalWorkItems.Count;
            var totalBugs = bugs.Count;

            // WorkItem status count (excluding bugs)
            var workItemStatusCount = new WorkItemStatusCountDto
            {
                Pending = normalWorkItems.Count(w => w.Status == "pending" || w.Status == "assigned" || w.Status == "reopened"),
                InProgress = normalWorkItems.Count(w => w.Status == "in_progress" || w.Status == "future_release"),
                Completed = normalWorkItems.Count(w => w.Status == "completed"),
                Testing = normalWorkItems.Count(w => w.Status == "testing"),
                BugFound = normalWorkItems.Count(w => w.Status == "bug_found"),
                Closed = normalWorkItems.Count(w => w.Status == "closed")
            };

            // Bug status count (only from Bug entities, since they already sync/include top-level bugs)
            var bugStatusCount = new BugStatusCountDto
            {
                Open = bugs.Count(b => b.Status == "open"),
                InProgress = bugs.Count(b => b.Status == "in_progress"),
                Fixed = bugs.Count(b => b.Status == "fixed"),
                Closed = bugs.Count(b => b.Status == "closed")
            };

            // Employee-wise workitem count (excluding bugs to match their dashboard)
            var employees = await _userRepo.Query()
                .Where(u => (u.UserType == "Employee" || u.UserType == "Both") && u.IsActive)
                .ToListAsync();

            var allNotes = await _noteRepo.Query()
                .Include(n => n.CreatedBy)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var employeeCounts = new List<EmployeeWorkItemCountDto>();

            foreach (var emp in employees)
            {
                var empWorkItems = normalWorkItems.Where(w => w.AssignedToUserId == emp.Id).ToList();
                var empNotes = allNotes.Where(n => n.EmployeeId == emp.Id).ToList();
                var latestNote = empNotes.FirstOrDefault();

                employeeCounts.Add(new EmployeeWorkItemCountDto
                {
                    EmployeeId = emp.Id,
                    EmployeeName = emp.Name,
                    TotalAssigned = empWorkItems.Count,
                    Pending = empWorkItems.Count(w => w.Status == "pending" || w.Status == "assigned" || w.Status == "reopened"),
                    InProgress = empWorkItems.Count(w => w.Status == "in_progress" || w.Status == "future_release"),
                    Completed = empWorkItems.Count(w => w.Status == "completed"),
                    Testing = empWorkItems.Count(w => w.Status == "testing"),
                    BugFound = empWorkItems.Count(w => w.Status == "bug_found"),
                    Closed = empWorkItems.Count(w => w.Status == "closed"),
                    AssignedTasks = empWorkItems.Select(w => new EmployeeTaskDto
                    {
                        Id = w.Id,
                        WorkNumber = w.WorkNumber,
                        Title = w.Title,
                        Status = w.Status,
                        Priority = w.Priority,
                        ProjectId = w.ProjectId,
                        ProjectName = w.Project?.Name ?? "Unknown Project",
                        ClientName = w.Project?.Client?.Name,
                        ProductName = w.Module?.Product?.Name,
                        ModuleName = w.Module?.Name
                    }).ToList(),
                    LatestNote = latestNote?.NoteText,
                    LatestNoteDate = latestNote?.CreatedAt,
                    NoteHistory = empNotes.Select(n => new DailyStatusNoteDto
                    {
                        Id = n.Id,
                        NoteText = n.NoteText,
                        CreatedAt = n.CreatedAt,
                        CreatedByName = n.CreatedBy?.Name ?? "Unknown PM"
                    }).ToList()
                });
            }

            return new DashboardResponseDto
            {
                TotalProjects = totalProjects,
                TotalWorkItems = totalWorkItems,
                TotalBugs = totalBugs,
                WorkItemStatusCount = workItemStatusCount,
                BugStatusCount = bugStatusCount,
                EmployeeWorkItemCounts = employeeCounts
            };
        }
    }
}