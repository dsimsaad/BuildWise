using BuildWise.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Controllers
{
    [Authorize]
    public class ConstructionController : Controller
    {
        private readonly BuildWiseDbContext _context;

        public ConstructionController(BuildWiseDbContext context)
        {
            _context = context;
        }

        public async System.Threading.Tasks.Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var projectId = await GetValidSelectedProjectIdAsync(userId);
            if (projectId == null)
            {
                TempData["WarningMessage"] = "Please select a specific project from the top navigation to manage construction progress.";
                return RedirectToAction("Index", "Projects");
            }

            ViewBag.PhaseTypes = await _context.PhaseTypes
                .AsNoTracking()
                .OrderBy(p => p.PhaseTypeId)
                .Select(p => new { p.PhaseTypeId, p.PhaseName })
                .ToListAsync();
            ViewBag.TaskStatuses = await _context.TaskStatuses
                .AsNoTracking()
                .OrderBy(s => s.StatusId)
                .Select(s => new { s.StatusId, s.StatusName })
                .ToListAsync();

            var project = await _context.Projects
                .AsNoTracking()
                .Include(p => p.Properties)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId.Value && p.UserId == userId);
            ViewBag.ProjectName = project?.ProjectName ?? "Selected Project";
            ViewBag.PropertyCount = project?.Properties.Count ?? 0;
            ViewBag.IsProjectCompleted = project?.IsCompleted == true;

            return View();
        }

        private int? GetSelectedProjectId()
        {
            return HttpContext.Session.GetInt32("SelectedProjectId");
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        private async System.Threading.Tasks.Task<int?> GetValidSelectedProjectIdAsync(int userId)
        {
            var selectedProjectId = GetSelectedProjectId();
            if (!selectedProjectId.HasValue)
            {
                return null;
            }

            var ownsProject = await _context.Projects
                .AsNoTracking()
                .AnyAsync(p => p.ProjectId == selectedProjectId.Value && p.UserId == userId);
            if (!ownsProject)
            {
                HttpContext.Session.Remove("SelectedProjectId");
                return null;
            }

            return selectedProjectId.Value;
        }

        private async System.Threading.Tasks.Task<bool> PhaseBelongsToUserAsync(int phaseId, int userId)
        {
            return await _context.Phases.AnyAsync(p => p.PhaseId == phaseId && p.Project.UserId == userId);
        }

        private async System.Threading.Tasks.Task<bool> TaskBelongsToUserAsync(int taskId, int userId)
        {
            return await _context.Tasks.AnyAsync(t => t.TaskId == taskId && t.Phase.Project.UserId == userId);
        }

        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> GetProgressData()
        {
            var userId = GetCurrentUserId();
            var projectId = await GetValidSelectedProjectIdAsync(userId);
            if (projectId == null)
            {
                return Json(new { phases = Array.Empty<object>(), overallProgress = 0, message = "Please select a project first." });
            }

            var phases = await _context.Phases
                .AsNoTracking()
                .Include(p => p.PhaseType)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Status)
                .Where(p => p.ProjectId == projectId.Value)
                .OrderBy(p => p.Sequence)
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var phaseDtos = phases.Select(p =>
            {
                var tasks = p.Tasks.OrderBy(t => t.CreatedAt).ToList();
                var completedTasks = tasks.Count(t => IsCompleted(t.Status?.StatusName, t.StatusId));
                var progress = tasks.Count > 0
                    ? Math.Round(completedTasks * 100m / tasks.Count, 2)
                    : (p.IsCompleted ? 100m : 0m);
                var phaseCompleted = tasks.Count > 0
                    ? completedTasks == tasks.Count
                    : p.IsCompleted;
                var displayName = string.IsNullOrWhiteSpace(p.CustomPhaseName)
                    ? p.PhaseType.PhaseName
                    : p.CustomPhaseName;

                return new
                {
                    phaseId = p.PhaseId,
                    phaseTypeId = p.PhaseTypeId,
                    phaseName = displayName,
                    sequence = p.Sequence,
                    startDate = p.StartDate?.ToString("yyyy-MM-dd"),
                    endDate = p.EndDate?.ToString("yyyy-MM-dd"),
                    isCompleted = phaseCompleted,
                    notes = p.Notes,
                    progress,
                    tasks = tasks.Select(t => new
                    {
                        taskId = t.TaskId,
                        phaseId = t.PhaseId,
                        taskName = t.TaskName,
                        description = t.Description,
                        statusId = t.StatusId,
                        status = t.Status?.StatusName ?? "Pending",
                        startDate = t.StartDate?.ToString("yyyy-MM-dd"),
                        endDate = t.EndDate?.ToString("yyyy-MM-dd"),
                        estimatedCost = t.EstimatedCost ?? 0,
                        isOverdue = t.EndDate.HasValue && t.EndDate.Value < today && !IsCompleted(t.Status?.StatusName, t.StatusId)
                    }).ToList()
                };
            }).ToList();

            var overallProgress = phaseDtos.Count > 0
                ? Math.Round(phaseDtos.Average(p => p.progress), 2)
                : 0m;

            return Json(new
            {
                phases = phaseDtos,
                overallProgress
            });
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> QuickSetup()
        {
            var userId = GetCurrentUserId();
            var projectId = await GetValidSelectedProjectIdAsync(userId);
            if (projectId == null)
                return Json(new { success = false, message = "Please select a project first." });

            var hasPhases = await _context.Phases.AnyAsync(p => p.ProjectId == projectId.Value);
            if (hasPhases)
                return Json(new { success = false, message = "This project already has phases. Add more phases manually if needed." });

            var preferredOrder = new[] { "Foundation", "Grey Structure", "Plumbing", "Electrical", "Tiling", "Painting", "Finishing" };
            var phaseTypes = await _context.PhaseTypes
                .Where(p => preferredOrder.Contains(p.PhaseName))
                .ToListAsync();

            var orderedTypes = preferredOrder
                .Select((name, index) => new { name, index })
                .Join(phaseTypes, order => order.name, type => type.PhaseName, (order, type) => new { order.index, type })
                .OrderBy(x => x.index)
                .ToList();

            if (!orderedTypes.Any())
                return Json(new { success = false, message = "Construction phase types are not configured." });

            var pendingStatusId = await _context.TaskStatuses
                .Where(s => s.StatusName == "Pending")
                .Select(s => (byte?)s.StatusId)
                .FirstOrDefaultAsync() ?? 1;
            var taskTemplates = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Foundation"] = new[] { "Site layout", "Excavation", "Foundation concrete" },
                ["Grey Structure"] = new[] { "Columns and beams", "Masonry work", "Roof slab" },
                ["Plumbing"] = new[] { "Water supply lines", "Drainage lines", "Fixture points" },
                ["Electrical"] = new[] { "Conduit layout", "Wiring", "DB and switch points" },
                ["Tiling"] = new[] { "Floor tiles", "Wall tiles", "Grouting" },
                ["Painting"] = new[] { "Surface preparation", "Primer", "Final coat" },
                ["Finishing"] = new[] { "Doors and windows", "Fixtures", "Final inspection" }
            };

            foreach (var item in orderedTypes)
            {
                var phase = new Phase
                {
                    ProjectId = projectId.Value,
                    PhaseTypeId = item.type.PhaseTypeId,
                    Sequence = (byte)(item.index + 1),
                    IsCompleted = false
                };

                if (taskTemplates.TryGetValue(item.type.PhaseName, out var templates))
                {
                    foreach (var template in templates)
                    {
                        phase.Tasks.Add(new Models.Task
                        {
                            TaskName = template,
                            StatusId = pendingStatusId,
                            EstimatedCost = 0,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                    }
                }

                _context.Phases.Add(phase);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> AddPhase([FromBody] PhaseRequest request)
        {
            var userId = GetCurrentUserId();
            var projectId = await GetValidSelectedProjectIdAsync(userId);
            if (projectId == null)
                return Json(new { success = false, message = "Please select a project first." });

            var phaseType = await _context.PhaseTypes.FirstOrDefaultAsync(p => p.PhaseTypeId == request.PhaseTypeId);
            if (phaseType == null)
                return Json(new { success = false, message = "Please select a valid phase type." });

            var dateError = ValidateDateRange(request.StartDate, request.EndDate, "phase");
            if (dateError != null)
                return Json(new { success = false, message = dateError });

            var sequence = request.Sequence;
            if (sequence <= 0)
            {
                var maxSequence = await _context.Phases
                    .Where(p => p.ProjectId == projectId.Value)
                    .Select(p => (byte?)p.Sequence)
                    .MaxAsync() ?? 0;
                sequence = (byte)Math.Min(byte.MaxValue, maxSequence + 1);
            }

            var customName = request.CustomPhaseName?.Trim();
            if (phaseType.PhaseName.Equals("Custom", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(customName))
                return Json(new { success = false, message = "Custom phase name is required." });

            var sequenceTaken = await _context.Phases.AnyAsync(p => p.ProjectId == projectId.Value && p.Sequence == sequence);
            if (sequenceTaken)
                return Json(new { success = false, message = "Another phase already uses this sequence number." });

            var isCustomPhase = phaseType.PhaseName.Equals("Custom", StringComparison.OrdinalIgnoreCase);
            _context.Phases.Add(new Phase
            {
                ProjectId = projectId.Value,
                PhaseTypeId = request.PhaseTypeId,
                CustomPhaseName = isCustomPhase && !string.IsNullOrWhiteSpace(customName) ? customName : null,
                Sequence = sequence,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsCompleted = request.IsCompleted,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> UpdatePhase([FromBody] PhaseRequest request)
        {
            var userId = GetCurrentUserId();
            var phase = await _context.Phases
                .Include(p => p.PhaseType)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Status)
                .FirstOrDefaultAsync(p => p.PhaseId == request.PhaseId && p.Project.UserId == userId);
            if (phase == null)
                return Forbid();

            var phaseType = await _context.PhaseTypes.FirstOrDefaultAsync(p => p.PhaseTypeId == request.PhaseTypeId);
            if (phaseType == null)
                return Json(new { success = false, message = "Please select a valid phase type." });

            var dateError = ValidateDateRange(request.StartDate, request.EndDate, "phase");
            if (dateError != null)
                return Json(new { success = false, message = dateError });

            var sequence = request.Sequence <= 0 ? phase.Sequence : request.Sequence;
            var sequenceTaken = await _context.Phases.AnyAsync(p =>
                p.ProjectId == phase.ProjectId && p.Sequence == sequence && p.PhaseId != phase.PhaseId);
            if (sequenceTaken)
                return Json(new { success = false, message = "Another phase already uses this sequence number." });

            var customName = request.CustomPhaseName?.Trim();
            if (phaseType.PhaseName.Equals("Custom", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(customName))
                return Json(new { success = false, message = "Custom phase name is required." });

            var isCustomPhase = phaseType.PhaseName.Equals("Custom", StringComparison.OrdinalIgnoreCase);
            phase.PhaseTypeId = request.PhaseTypeId;
            phase.CustomPhaseName = isCustomPhase && !string.IsNullOrWhiteSpace(customName) ? customName : null;
            phase.Sequence = sequence;
            phase.StartDate = request.StartDate;
            phase.EndDate = request.EndDate;
            phase.IsCompleted = phase.Tasks.Any()
                ? phase.Tasks.All(t => IsCompleted(t.Status?.StatusName, t.StatusId))
                : request.IsCompleted;
            phase.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> DeletePhase(int id)
        {
            var userId = GetCurrentUserId();
            var phase = await _context.Phases
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.PhaseId == id && p.Project.UserId == userId);
            if (phase == null)
                return Forbid();

            var hasMaterialUsage = await _context.MaterialUsages.AnyAsync(u => u.PhaseId == id);
            var hasExpenses = await _context.Expenses.AnyAsync(e => e.PhaseId == id);
            if (hasMaterialUsage || hasExpenses)
            {
                return Json(new
                {
                    success = false,
                    message = "This phase has material usage or expenses linked to it. Keep it for reporting instead of deleting it."
                });
            }

            _context.Tasks.RemoveRange(phase.Tasks);
            _context.Phases.Remove(phase);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> AddTask([FromBody] TaskRequest request)
        {
            var userId = GetCurrentUserId();
            if (!await PhaseBelongsToUserAsync(request.PhaseId, userId))
                return Forbid();

            if (string.IsNullOrWhiteSpace(request.TaskName))
                return Json(new { success = false, message = "Task name is required." });

            var statusId = await ResolveStatusIdAsync(request.StatusId, request.Status);
            var dateError = ValidateDateRange(request.StartDate, request.EndDate, "task");
            if (dateError != null)
                return Json(new { success = false, message = dateError });

            _context.Tasks.Add(new Models.Task
            {
                PhaseId = request.PhaseId,
                TaskName = request.TaskName.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                StatusId = statusId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                EstimatedCost = request.EstimatedCost,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await RefreshPhaseCompletionAsync(request.PhaseId);
            return Json(new { success = true });
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> UpdateTask([FromBody] TaskRequest request)
        {
            var userId = GetCurrentUserId();
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == request.TaskId && t.Phase.Project.UserId == userId);
            if (task == null)
                return Forbid();

            if (string.IsNullOrWhiteSpace(request.TaskName))
                return Json(new { success = false, message = "Task name is required." });

            var dateError = ValidateDateRange(request.StartDate, request.EndDate, "task");
            if (dateError != null)
                return Json(new { success = false, message = dateError });

            task.TaskName = request.TaskName.Trim();
            task.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            task.StatusId = await ResolveStatusIdAsync(request.StatusId, request.Status);
            task.StartDate = request.StartDate;
            task.EndDate = request.EndDate;
            task.EstimatedCost = request.EstimatedCost;
            task.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await RefreshPhaseCompletionAsync(task.PhaseId);
            return Json(new { success = true });
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetCurrentUserId();
            if (!await TaskBelongsToUserAsync(id, userId))
                return Forbid();

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return Json(new { success = false });

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            await RefreshPhaseCompletionAsync(task.PhaseId);
            return Json(new { success = true });
        }

        private static bool IsCompleted(string? statusName, byte statusId)
        {
            return statusId == 3 || string.Equals(statusName, "Completed", StringComparison.OrdinalIgnoreCase);
        }

        private async System.Threading.Tasks.Task<byte> ResolveStatusIdAsync(byte? statusId, string? statusName)
        {
            if (statusId.HasValue && await _context.TaskStatuses.AnyAsync(s => s.StatusId == statusId.Value))
                return statusId.Value;

            if (!string.IsNullOrWhiteSpace(statusName))
            {
                var existing = await _context.TaskStatuses
                    .FirstOrDefaultAsync(s => s.StatusName == statusName.Trim());
                if (existing != null)
                    return existing.StatusId;
            }

            return 1;
        }

        private static string? ValidateDateRange(DateOnly? startDate, DateOnly? endDate, string label)
        {
            if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
                return $"The {label} end date cannot be before the start date.";

            return null;
        }

        private async System.Threading.Tasks.Task RefreshPhaseCompletionAsync(int phaseId)
        {
            var phase = await _context.Phases
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Status)
                .FirstOrDefaultAsync(p => p.PhaseId == phaseId);
            if (phase == null || !phase.Tasks.Any())
                return;

            phase.IsCompleted = phase.Tasks.All(t => IsCompleted(t.Status?.StatusName, t.StatusId));
            await _context.SaveChangesAsync();
        }

        public class PhaseRequest
        {
            public int PhaseId { get; set; }
            public byte PhaseTypeId { get; set; }
            public string? CustomPhaseName { get; set; }
            public byte Sequence { get; set; }
            public DateOnly? StartDate { get; set; }
            public DateOnly? EndDate { get; set; }
            public bool IsCompleted { get; set; }
            public string? Notes { get; set; }
        }

        public class TaskRequest
        {
            public int TaskId { get; set; }
            public int PhaseId { get; set; }
            public string? TaskName { get; set; }
            public string? Description { get; set; }
            public byte? StatusId { get; set; }
            public string? Status { get; set; }
            public DateOnly? StartDate { get; set; }
            public DateOnly? EndDate { get; set; }
            public decimal? EstimatedCost { get; set; }
        }
    }
}
