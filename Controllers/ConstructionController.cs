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

            var result = await CreatePhaseAsync(request, projectId.Value);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            return Json(new { success = true });
        }

        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> AddPhase()
        {
            var userId = GetCurrentUserId();
            var projectId = await GetValidSelectedProjectIdAsync(userId);
            if (projectId == null)
            {
                TempData["WarningMessage"] = "Please select a specific project from the top navigation to add a phase.";
                return RedirectToAction("Index", "Projects");
            }

            await PopulateAddPhasePageAsync(projectId.Value, userId);
            return View(new PhaseRequest
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<IActionResult> AddPhasePage(PhaseRequest request)
        {
            var userId = GetCurrentUserId();
            var projectId = await GetValidSelectedProjectIdAsync(userId);
            if (projectId == null)
            {
                TempData["WarningMessage"] = "Please select a specific project from the top navigation to add a phase.";
                return RedirectToAction("Index", "Projects");
            }

            var result = await CreatePhaseAsync(request, projectId.Value);
            if (result.Success)
                return RedirectToAction(nameof(Index));

            ModelState.AddModelError("", result.Message ?? "Unable to add phase.");
            await PopulateAddPhasePageAsync(projectId.Value, userId);
            return View("AddPhase", request);
        }

        private async System.Threading.Tasks.Task PopulateAddPhasePageAsync(int projectId, int userId)
        {
            ViewBag.PhaseTypes = await _context.PhaseTypes
                .AsNoTracking()
                .OrderBy(p => p.PhaseTypeId)
                .Select(p => new { p.PhaseTypeId, p.PhaseName })
                .ToListAsync();

            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.UserId == userId);
            ViewBag.ProjectName = project?.ProjectName ?? "Selected Project";
        }

        private async System.Threading.Tasks.Task<(bool Success, string? Message)> CreatePhaseAsync(PhaseRequest request, int projectId)
        {
            var phaseType = await _context.PhaseTypes.FirstOrDefaultAsync(p => p.PhaseTypeId == request.PhaseTypeId);
            if (phaseType == null)
                return (false, "Please select a valid phase type.");

            var sequence = request.Sequence;
            if (sequence <= 0)
            {
                var maxSequence = await _context.Phases
                    .Where(p => p.ProjectId == projectId)
                    .Select(p => (byte?)p.Sequence)
                    .MaxAsync() ?? 0;
                sequence = (byte)Math.Min(byte.MaxValue, maxSequence + 1);
            }

            var customName = request.CustomPhaseName?.Trim();
            if (phaseType.PhaseName.Equals("Custom", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(customName))
                return (false, "Custom phase name is required.");

            var sequenceTaken = await _context.Phases.AnyAsync(p => p.ProjectId == projectId && p.Sequence == sequence);
            if (sequenceTaken)
                return (false, "Another phase already uses this sequence number.");

            var today = DateOnly.FromDateTime(DateTime.Today);
            var startDate = request.StartDate ?? today;
            var isCustomPhase = phaseType.PhaseName.Equals("Custom", StringComparison.OrdinalIgnoreCase);
            var isCompleted = request.IsCompleted;
            if (isCompleted && startDate > today)
                return (false, "A completed phase cannot start in the future.");

            _context.Phases.Add(new Phase
            {
                ProjectId = projectId,
                PhaseTypeId = request.PhaseTypeId,
                CustomPhaseName = isCustomPhase && !string.IsNullOrWhiteSpace(customName) ? customName : null,
                Sequence = sequence,
                StartDate = startDate,
                EndDate = isCompleted ? today : null,
                IsCompleted = isCompleted,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            });

            await _context.SaveChangesAsync();
            return (true, null);
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
            var isCompleted = phase.Tasks.Any()
                ? phase.Tasks.All(t => IsCompleted(t.Status?.StatusName, t.StatusId))
                : request.IsCompleted;
            if (isCompleted && request.StartDate.HasValue && request.StartDate.Value > DateOnly.FromDateTime(DateTime.Today))
                return Json(new { success = false, message = "A completed phase cannot start in the future." });

            phase.IsCompleted = isCompleted;
            phase.EndDate = isCompleted
                ? phase.EndDate ?? DateOnly.FromDateTime(DateTime.Today)
                : null;
            phase.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> ReorderPhases([FromBody] PhaseOrderRequest request)
        {
            var userId = GetCurrentUserId();
            var projectId = await GetValidSelectedProjectIdAsync(userId);
            if (projectId == null)
                return Json(new { success = false, message = "Please select a project first." });

            var requestedIds = request?.PhaseIds?.Distinct().ToList() ?? new List<int>();
            if (!requestedIds.Any())
                return Json(new { success = false, message = "No phases were provided." });

            if (requestedIds.Count > byte.MaxValue)
                return Json(new { success = false, message = "Too many phases to reorder." });

            var phases = await _context.Phases
                .Where(p => p.ProjectId == projectId.Value && p.Project.UserId == userId)
                .ToListAsync();

            if (phases.Count != requestedIds.Count || phases.Any(p => !requestedIds.Contains(p.PhaseId)))
                return Json(new { success = false, message = "Phase order is out of date. Refresh and try again." });

            var maxExistingSequence = phases.Max(p => p.Sequence);
            var tempStart = byte.MaxValue - requestedIds.Count + 1;
            if (tempStart <= maxExistingSequence)
                return Json(new { success = false, message = "Too many phases to reorder safely." });

            var phaseMap = phases.ToDictionary(p => p.PhaseId);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            for (var i = 0; i < requestedIds.Count; i++)
            {
                phaseMap[requestedIds[i]].Sequence = (byte)(byte.MaxValue - i);
            }

            await _context.SaveChangesAsync();

            for (var i = 0; i < requestedIds.Count; i++)
            {
                phaseMap[requestedIds[i]].Sequence = (byte)(i + 1);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
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
            phase.EndDate = phase.IsCompleted
                ? phase.EndDate ?? DateOnly.FromDateTime(DateTime.Today)
                : null;
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

        public class PhaseOrderRequest
        {
            public List<int> PhaseIds { get; set; } = new();
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
