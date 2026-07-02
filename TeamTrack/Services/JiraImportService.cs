using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TeamTrack.Data;
using TeamTrack.DTOs;
using TeamTrack.Models;
using TeamTrack.Repositories;

namespace TeamTrack.Services
{
    public class JiraImportService : IJiraImportService
    {
        private readonly IRepository<Project> _projectRepo;
        private readonly IRepository<WorkItem> _workItemRepo;
        private readonly IRepository<Bug> _bugRepo;
        private readonly IRepository<User> _userRepo;

        public JiraImportService(
            IRepository<Project> projectRepo,
            IRepository<WorkItem> workItemRepo,
            IRepository<Bug> bugRepo,
            IRepository<User> userRepo)
        {
            _projectRepo = projectRepo;
            _workItemRepo = workItemRepo;
            _bugRepo = bugRepo;
            _userRepo = userRepo;
        }

        public async Task<JiraImportResponseDto> ImportJiraCsvAsync(Stream csvStream, int currentUserId)
        {
            var response = new JiraImportResponseDto();

            try
            {
                // 1. Parse CSV into raw string records
                var records = ParseCsv(csvStream);
                if (records.Count <= 1)
                {
                    response.Errors.Add("CSV file is empty or only contains headers.");
                    return response;
                }

                var header = records[0];
                var colMap = new Dictionary<string, int>();
                for (int i = 0; i < header.Count; i++)
                {
                    var colName = header[i].Trim().ToLower();
                    if (!colMap.ContainsKey(colName))
                    {
                        colMap[colName] = i;
                    }
                }

                // Helper to get value dynamically by checking possible column names
                string GetValue(List<string> row, string[] possibleNames)
                {
                    foreach (var name in possibleNames)
                    {
                        if (colMap.TryGetValue(name.ToLower(), out var index) && index < row.Count)
                        {
                            return row[index].Trim();
                        }
                    }
                    return string.Empty;
                }

                // Lookups to avoid repeated database hits and resolve duplicates
                var userCache = await _userRepo.Query().ToDictionaryAsync(u => u.Name.ToLower(), u => u.Id);
                var projectCache = await _projectRepo.Query().ToDictionaryAsync(p => p.ProjectNumber.ToLower(), p => p.Id);
                var workItemCache = await _workItemRepo.Query().ToDictionaryAsync(w => w.WorkNumber.ToLower(), w => w.Id);

                // We will collect parent keys for subtask mapping
                var parentKeyMap = new Dictionary<string, string>(); // subtask key -> parent key
                var jiraKeyToDbIdMap = new Dictionary<string, int>(); // jira key -> db WorkItemId

                // Lists to hold items parsed from rows
                var rowsToProcess = records.Skip(1).Where(r => r.Any(val => !string.IsNullOrEmpty(val))).ToList();

                // Pass 1: Parse and create Projects, Users, and non-Subtask WorkItems
                foreach (var row in rowsToProcess)
                {
                    var summary = GetValue(row, ["summary", "issue summary", "title"]);
                    var issueKey = GetValue(row, ["issue key", "key"]);
                    var issueType = GetValue(row, ["issue type", "type"]);
                    var status = GetValue(row, ["status", "issue status"]);
                    var projectKey = GetValue(row, ["project key", "project code"]);
                    var projectName = GetValue(row, ["project name", "project"]);
                    var priority = GetValue(row, ["priority", "issue priority"]);
                    var assignee = GetValue(row, ["assignee", "assigned to"]);
                    var assigneeEmail = GetValue(row, ["assignee email", "assignee e-mail", "user email", "email"]);
                    var description = GetValue(row, ["description"]);
                    var createdStr = GetValue(row, ["created", "created date"]);
                    var resolvedStr = GetValue(row, ["resolved", "resolved date", "resolution date"]);
                    var dueDateStr = GetValue(row, ["due date", "duedate"]);
                    var parentKey = GetValue(row, ["parent", "parent key", "parent id"]);
                    var labels = GetValue(row, ["labels", "label"]);
                    var team = GetValue(row, ["team", "custom field (team)"]);

                    if (string.IsNullOrEmpty(summary) && string.IsNullOrEmpty(issueKey))
                    {
                        continue; // Skip completely empty rows
                    }

                    // 1. Resolve Project
                    if (string.IsNullOrEmpty(projectKey)) projectKey = "JIRA";
                    if (string.IsNullOrEmpty(projectName)) projectName = "Jira Import Project";

                    if (!projectCache.TryGetValue(projectKey.ToLower(), out var projectId))
                    {
                        // Check if project exists by name first
                        var existingProject = await _projectRepo.GetAsync(p => p.Name.ToLower() == projectName.ToLower());
                        if (existingProject != null)
                        {
                            projectId = existingProject.Id;
                            projectCache[projectKey.ToLower()] = projectId;
                        }
                        else
                        {
                            // Create new Project
                            var newProject = new Project
                            {
                                ProjectNumber = projectKey.ToUpper(),
                                Name = projectName,
                                Description = "Imported from Jira",
                                Status = "active",
                                CreatedByUserId = currentUserId,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            await _projectRepo.AddAsync(newProject);
                            await _projectRepo.SaveAsync();

                            projectId = newProject.Id;
                            projectCache[projectKey.ToLower()] = projectId;
                            response.ProjectsImported++;
                        }
                    }

                    // 2. Resolve Assignee User
                    // Priority order:
                    //   a) If "Assignee Email" column exists in CSV → use it directly
                    //   b) If "Assignee" column contains an email (has @) → use it directly
                    //   c) Try to match by display name against existing users
                    //   d) Fallback: create with name@teamtrack.com
                    int? assigneeId = null;
                    if (!string.IsNullOrEmpty(assignee))
                    {
                        var cacheKey = assignee.ToLower();
                        if (!userCache.TryGetValue(cacheKey, out var userId))
                        {
                            var displayName = assignee.Trim();
                            string resolvedEmail;

                            if (!string.IsNullOrEmpty(assigneeEmail) && assigneeEmail.Contains("@"))
                            {
                                // Use the dedicated email column from the CSV
                                resolvedEmail = assigneeEmail.Trim().ToLower();
                            }
                            else if (assignee.Trim().Contains("@"))
                            {
                                // Assignee field itself is an email
                                resolvedEmail = assignee.Trim().ToLower();
                                var partBeforeAt = resolvedEmail.Split('@')[0];
                                if (!string.IsNullOrEmpty(partBeforeAt))
                                {
                                    var parts = partBeforeAt.Split('.');
                                    displayName = string.Join(" ", parts.Select(p => p.Length > 0 ? char.ToUpper(p[0]) + p.Substring(1) : ""));
                                }
                            }
                            else
                            {
                                // No email info in CSV — try to match by display name in DB first
                                var existingByName = await _userRepo.GetAsync(u => u.Name.ToLower() == displayName.ToLower());
                                if (existingByName != null)
                                {
                                    userCache[cacheKey] = existingByName.Id;
                                    assigneeId = existingByName.Id;
                                    goto afterUserResolution;
                                }
                                // Fallback: generate email from name
                                resolvedEmail = displayName.Replace(" ", "").ToLower() + "@teamtrack.com";
                            }

                            var existingUserByEmail = await _userRepo.GetAsync(u => u.Email.ToLower() == resolvedEmail);
                            if (existingUserByEmail != null)
                            {
                                userId = existingUserByEmail.Id;
                                userCache[cacheKey] = userId;
                            }
                            else
                            {
                                var newUser = new User
                                {
                                    Name = displayName,
                                    Email = resolvedEmail,
                                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("DefaultPass123"),
                                    UserType = "Employee",
                                    IsActive = true,
                                    CreatedAt = DateTime.UtcNow
                                };
                                await _userRepo.AddAsync(newUser);
                                await _userRepo.SaveAsync();

                                userId = newUser.Id;
                                userCache[cacheKey] = userId;
                                response.UsersImported++;
                            }
                        }
                        assigneeId = userId;
                        afterUserResolution:;
                    }

                    // 3. Link project members
                    if (assigneeId.HasValue)
                    {
                        var project = await _projectRepo.Query()
                            .Include(p => p.AssignedEmployees)
                            .FirstOrDefaultAsync(p => p.Id == projectId);
                        if (project != null && !project.AssignedEmployees.Any(e => e.Id == assigneeId.Value))
                        {
                            var userObj = await _userRepo.GetAsync(u => u.Id == assigneeId.Value);
                            if (userObj != null)
                            {
                                project.AssignedEmployees.Add(userObj);
                                await _projectRepo.SaveAsync();
                            }
                        }
                    }

                    // Store subtask parent reference for second pass
                    if (!string.IsNullOrEmpty(parentKey) && !string.IsNullOrEmpty(issueKey))
                    {
                        parentKeyMap[issueKey] = parentKey;
                    }

                    // Map Status
                    var mappedStatus = MapStatus(status);
                    var mappedPriority = MapPriority(priority);
                    var createdTime = ParseDate(createdStr) ?? DateTime.UtcNow;
                    var resolvedTime = ParseDate(resolvedStr);
                    var dueTime = ParseDate(dueDateStr);

                    // 4. Create or Update WorkItem (If it is not a subtask, we import it now. Subtasks will be imported in Pass 2 to ensure parents exist)
                    var isSubtask = issueType.Equals("Sub-task", StringComparison.OrdinalIgnoreCase) || 
                                    issueType.Equals("Subtask", StringComparison.OrdinalIgnoreCase);

                    if (!isSubtask)
                    {
                        if (workItemCache.TryGetValue(issueKey.ToLower(), out var existingWorkItemId))
                        {
                            // Update existing WorkItem
                            var existingWorkItem = await _workItemRepo.GetAsync(w => w.Id == existingWorkItemId);
                            if (existingWorkItem != null)
                            {
                                existingWorkItem.Title = summary;
                                existingWorkItem.Description = description;
                                existingWorkItem.Status = mappedStatus;
                                existingWorkItem.Priority = mappedPriority;
                                existingWorkItem.WorkType = issueType;
                                existingWorkItem.AssignedToUserId = assigneeId;
                                existingWorkItem.Labels = labels;
                                existingWorkItem.Team = team;
                                existingWorkItem.DueDate = dueTime;
                                existingWorkItem.CompletedAt = resolvedTime ?? (mappedStatus == "completed" ? DateTime.UtcNow : null);
                                existingWorkItem.UpdatedAt = DateTime.UtcNow;

                                await _workItemRepo.SaveAsync();
                                jiraKeyToDbIdMap[issueKey] = existingWorkItem.Id;
                            }
                        }
                        else
                        {
                            // Create new WorkItem
                            var newWorkItem = new WorkItem
                            {
                                WorkNumber = issueKey.ToUpper(),
                                Title = summary,
                                Description = description,
                                Status = mappedStatus,
                                Priority = mappedPriority,
                                WorkType = string.IsNullOrEmpty(issueType) ? "Task" : issueType,
                                ProjectId = projectId,
                                CreatedByUserId = currentUserId,
                                AssignedToUserId = assigneeId,
                                CreatedAt = createdTime,
                                UpdatedAt = DateTime.UtcNow,
                                CompletedAt = resolvedTime ?? (mappedStatus == "completed" ? DateTime.UtcNow : null),
                                DueDate = dueTime,
                                Labels = labels,
                                Team = team
                            };

                            await _workItemRepo.AddAsync(newWorkItem);
                            await _workItemRepo.SaveAsync();

                            workItemCache[issueKey.ToLower()] = newWorkItem.Id;
                            jiraKeyToDbIdMap[issueKey] = newWorkItem.Id;
                            response.WorkItemsImported++;

                            // If type is Bug, also register in bugs table
                            if (issueType.Equals("Bug", StringComparison.OrdinalIgnoreCase))
                            {
                                response.BugsImported++;
                            }
                        }
                    }
                }

                // Pass 2: Parse and create Subtasks, linking them to Parent WorkItems
                foreach (var row in rowsToProcess)
                {
                    var issueType = GetValue(row, ["issue type", "type"]);
                    var isSubtask = issueType.Equals("Sub-task", StringComparison.OrdinalIgnoreCase) || 
                                    issueType.Equals("Subtask", StringComparison.OrdinalIgnoreCase);

                    if (!isSubtask) continue;

                    var summary = GetValue(row, ["summary", "issue summary", "title"]);
                    var issueKey = GetValue(row, ["issue key", "key"]);
                    var status = GetValue(row, ["status", "issue status"]);
                    var projectKey = GetValue(row, ["project key", "project code"]);
                    var priority = GetValue(row, ["priority", "issue priority"]);
                    var assignee = GetValue(row, ["assignee", "assigned to"]);
                    var assigneeEmail = GetValue(row, ["assignee email", "assignee e-mail", "user email", "email"]);
                    var description = GetValue(row, ["description"]);
                    var createdStr = GetValue(row, ["created", "created date"]);
                    var resolvedStr = GetValue(row, ["resolved", "resolved date", "resolution date"]);
                    var dueDateStr = GetValue(row, ["due date", "duedate"]);
                    var labels = GetValue(row, ["labels", "label"]);
                    var team = GetValue(row, ["team", "custom field (team)"]);

                    // Find parent DB ID using ParentKeyMap
                    int? parentDbId = null;
                    if (parentKeyMap.TryGetValue(issueKey, out var parentKey) && !string.IsNullOrEmpty(parentKey))
                    {
                        if (jiraKeyToDbIdMap.TryGetValue(parentKey, out var pId))
                        {
                            parentDbId = pId;
                        }
                        else
                        {
                            // If parent not in memory, try searching database by JIRA Issue Key
                            var parentItem = await _workItemRepo.GetAsync(w => w.WorkNumber.ToLower() == parentKey.ToLower());
                            if (parentItem != null)
                            {
                                parentDbId = parentItem.Id;
                                jiraKeyToDbIdMap[parentKey] = parentItem.Id;
                            }
                        }
                    }

                    // Resolve Project
                    if (string.IsNullOrEmpty(projectKey)) projectKey = "JIRA";
                    projectCache.TryGetValue(projectKey.ToLower(), out var projectId);

                    // Resolve Assignee User
                    int? assigneeId = null;
                    if (!string.IsNullOrEmpty(assignee))
                    {
                        userCache.TryGetValue(assignee.ToLower(), out var userId);
                        assigneeId = userId > 0 ? userId : (int?)null;
                    }

                    var mappedStatus = MapStatus(status);
                    var mappedPriority = MapPriority(priority);
                    var createdTime = ParseDate(createdStr) ?? DateTime.UtcNow;
                    var resolvedTime = ParseDate(resolvedStr);
                    var dueTime = ParseDate(dueDateStr);

                    if (workItemCache.TryGetValue(issueKey.ToLower(), out var existingWorkItemId))
                    {
                        var existingWorkItem = await _workItemRepo.GetAsync(w => w.Id == existingWorkItemId);
                        if (existingWorkItem != null)
                        {
                            existingWorkItem.Title = summary;
                            existingWorkItem.Description = description;
                            existingWorkItem.Status = mappedStatus;
                            existingWorkItem.Priority = mappedPriority;
                            existingWorkItem.WorkType = "Subtask";
                            existingWorkItem.ParentId = parentDbId;
                            existingWorkItem.AssignedToUserId = assigneeId;
                            existingWorkItem.Labels = labels;
                            existingWorkItem.Team = team;
                            existingWorkItem.DueDate = dueTime;
                            existingWorkItem.CompletedAt = resolvedTime ?? (mappedStatus == "completed" ? DateTime.UtcNow : null);
                            existingWorkItem.UpdatedAt = DateTime.UtcNow;

                            await _workItemRepo.SaveAsync();
                        }
                    }
                    else
                    {
                        var newWorkItem = new WorkItem
                        {
                            WorkNumber = issueKey.ToUpper(),
                            Title = summary,
                            Description = description,
                            Status = mappedStatus,
                            Priority = mappedPriority,
                            WorkType = "Subtask",
                            ParentId = parentDbId,
                            ProjectId = projectId == 0 ? 1 : projectId, // Fallback to Project ID 1 if not found
                            CreatedByUserId = currentUserId,
                            AssignedToUserId = assigneeId,
                            CreatedAt = createdTime,
                            UpdatedAt = DateTime.UtcNow,
                            CompletedAt = resolvedTime ?? (mappedStatus == "completed" ? DateTime.UtcNow : null),
                            DueDate = dueTime,
                            Labels = labels,
                            Team = team
                        };

                        await _workItemRepo.AddAsync(newWorkItem);
                        await _workItemRepo.SaveAsync();

                        workItemCache[issueKey.ToLower()] = newWorkItem.Id;
                        response.WorkItemsImported++;
                    }
                }
            }
            catch (Exception ex)
            {
                response.Errors.Add($"Critical error during import: {ex.Message}");
            }

            return response;
        }

        private static List<List<string>> ParseCsv(Stream stream)
        {
            var records = new List<List<string>>();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var currentRecord = new List<string>();
            var currentField = new StringBuilder();
            var inQuotes = false;

            int nextChar;
            while ((nextChar = reader.Read()) != -1)
            {
                char c = (char)nextChar;

                if (c == '"')
                {
                    int peek = reader.Peek();
                    if (inQuotes && peek == '"')
                    {
                        currentField.Append('"');
                        reader.Read(); // Consume the peeked quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    currentRecord.Add(currentField.ToString());
                    currentField.Clear();
                }
                else if (c == '\r' && !inQuotes)
                {
                    if (reader.Peek() == '\n')
                    {
                        reader.Read(); // Consume '\n'
                    }
                    currentRecord.Add(currentField.ToString());
                    records.Add(currentRecord);
                    currentRecord = new List<string>();
                    currentField.Clear();
                }
                else if (c == '\n' && !inQuotes)
                {
                    currentRecord.Add(currentField.ToString());
                    records.Add(currentRecord);
                    currentRecord = new List<string>();
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            if (currentField.Length > 0 || currentRecord.Count > 0)
            {
                currentRecord.Add(currentField.ToString());
                records.Add(currentRecord);
            }

            return records;
        }

        private static string MapStatus(string jiraStatus)
        {
            if (string.IsNullOrWhiteSpace(jiraStatus)) return "pending";

            var normalized = jiraStatus.ToLower();

            if (normalized.Contains("progress")) return "in_progress";
            if (normalized.Contains("reopen")) return "reopened";
            if (normalized.Contains("assign")) return "assigned";
            if (normalized.Contains("resolved") || normalized.Contains("fixed") || normalized.Contains("done") || normalized.Contains("completed") || normalized.Contains("closed")) return "completed";
            if (normalized.Contains("future")) return "future_release";
            if (normalized.Contains("customer") || normalized.Contains("waiting")) return "waiting_customer";

            return "pending";
        }

        private static string MapPriority(string jiraPriority)
        {
            if (string.IsNullOrWhiteSpace(jiraPriority)) return "medium";

            var normalized = jiraPriority.ToLower();
            if (normalized.Contains("high") || normalized.Contains("major") || normalized.Contains("critical") || normalized.Contains("blocker")) return "high";
            if (normalized.Contains("low") || normalized.Contains("minor") || normalized.Contains("trivial")) return "low";

            return "medium";
        }

        private static DateTime? ParseDate(string? dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return null;

            if (DateTime.TryParse(dateStr, out var parsedDate))
            {
                return DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }

            string[] formats = {
                "dd/MMM/yy h:mm tt", "dd/MMM/yy H:mm",
                "d/MMM/yy h:mm tt", "d/MMM/yy H:mm",
                "dd/MM/yy h:mm tt", "dd/MM/yy H:mm",
                "d/M/yy h:mm tt", "d/M/yy H:mm",
                "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"
            };

            if (DateTime.TryParseExact(dateStr, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var exactDate))
            {
                return DateTime.SpecifyKind(exactDate, DateTimeKind.Utc);
            }

            return null;
        }
    }
}
