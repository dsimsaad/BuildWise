using BuildWise.Models;
using BuildWise.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Controllers
{
    [Authorize]
    public class ConstructionController : BaseController
    {
        private readonly BuildWiseDbContext _context;
        private readonly PropertyPhaseSchemaService _propertyPhaseSchema;

        public ConstructionController(BuildWiseDbContext context, PropertyPhaseSchemaService propertyPhaseSchema)
        {
            _context = context;
            _propertyPhaseSchema = propertyPhaseSchema;
        }

        public async System.Threading.Tasks.Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            await _propertyPhaseSchema.EnsureAsync(HttpContext.RequestAborted);
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
            ViewBag.ProjectProperties = await GetProjectPropertiesAsync(projectId.Value, userId);

            return View();
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
            await _propertyPhaseSchema.EnsureAsync(HttpContext.RequestAborted);
            var projectId = await GetValidSelectedProjectIdAsync(userId);
            if (projectId == null)
            {
                return Json(new { phases = Array.Empty<object>(), overallProgress = 0, message = "Please select a project first." });
            }

            await SyncCompletedPropertyPhasesAsync(projectId.Value, userId);

            var phases = await _context.Phases
                .AsNoTracking()
                .Include(p => p.PhaseType)
                .Include(p => p.Property)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Status)
                .Where(p => p.ProjectId == projectId.Value)
                .OrderBy(p => p.PropertyId == null ? 1 : 0)
                .ThenBy(p => p.Property != null ? p.Property.PropertyName : "")
                .ThenBy(p => p.Sequence)
                .ToListAsync();

            var properties = await GetProjectPropertiesAsync(projectId.Value, userId);

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
                    propertyId = p.PropertyId,
                    propertyName = p.Property?.PropertyName ?? "Unassigned",
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
                        isOverdue = t.EndDate.HasValue && t.EndDate.Value < today && !IsCompleted(t.Status?.StatusName, t.StatusId)
                    }).ToList()
                };
            }).ToList();

            var propertyProgress = properties
                .Select(property =>
                {
                    var propertyPhases = phaseDtos.Where(p => p.propertyId == property.PropertyId).ToList();
                    var propertyCompleted = string.Equals(property.Status?.StatusName, "Completed", StringComparison.OrdinalIgnoreCase);
                    var progress = propertyPhases.Count > 0
                        ? Math.Round(propertyPhases.Average(p => p.progress), 2)
                        : propertyCompleted ? 100m : 0m;
                    return new
                    {
                        propertyId = property.PropertyId,
                        propertyName = property.PropertyName,
                        status = property.Status?.StatusName ?? "",
                        phaseCount = propertyPhases.Count,
                        progress
                    };
                })
                .ToList();

            var overallProgress = propertyProgress.Any()
                ? Math.Round(propertyProgress.Average(p => p.progress), 2)
                : phaseDtos.Count > 0 ? Math.Round(phaseDtos.Average(p => p.progress), 2) : 0m;

            return Json(new
            {
                phases = phaseDtos,
                properties = propertyProgress,
                overallProgress
            });
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> QuickSetup()
        {
            var userId = GetCurrentUserId();
            await _propertyPhaseSchema.EnsureAsync(HttpContext.RequestAborted);
            var projectId = await GetValidSelectedProjectIdAsync(userId);
            if (projectId == null)
                return Json(new { success = false, message = "Please select a project first." });

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

            var properties = await GetProjectPropertiesAsync(projectId.Value, userId);
            var propertyIds = properties.Select(p => (int?)p.PropertyId).ToList();
            if (!propertyIds.Any())
                return Json(new { success = false, message = "Add or link at least one property before creating construction phases." });

            var existingPhaseKeys = await _context.Phases
                .Where(p => p.ProjectId == projectId.Value)
                .Select(p => new { p.PropertyId, p.PhaseTypeId })
                .ToListAsync();

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

            var nextSequence = (await _context.Phases
                .Where(p => p.ProjectId == projectId.Value)
                .Select(p => (byte?)p.Sequence)
                .MaxAsync() ?? 0) + 1;
            var created = 0;

            foreach (var propertyId in propertyIds)
            {
                foreach (var item in orderedTypes)
                {
                    if (existingPhaseKeys.Any(p => p.PropertyId == propertyId && p.PhaseTypeId == item.type.PhaseTypeId))
                        continue;

                    var phase = new Phase
                    {
                        ProjectId = projectId.Value,
                        PropertyId = propertyId,
                        PhaseTypeId = item.type.PhaseTypeId,
                        Sequence = (byte)Math.Min(byte.MaxValue, nextSequence++),
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
                    created++;
                }
            }

            if (created == 0)
                return Json(new { success = false, message = "All linked properties already have the standard phase sequence." });

            await _context.SaveChangesAsync();
            foreach (var propertyId in propertyIds.Where(id => id.HasValue).Select(id => id.GetValueOrDefault()).Distinct())
            {
                await RefreshPropertyAndProjectCompletionAsync(propertyId, projectId.Value, userId);
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> AddPhase([FromBody] PhaseRequest request)
        {
            var userId = GetCurrentUserId();
            await _propertyPhaseSchema.EnsureAsync(HttpContext.RequestAborted);
            var projectId = await GetValidSelectedProjectIdAsync(userId);
            if (projectId == null)
                return Json(new { success = false, message = "Please select a project first." });

            var result = await CreatePhaseAsync(request, projectId.Value, userId);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            return Json(new { success = true });
        }

        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> AddPhase()
        {
            var userId = GetCurrentUserId();
            await _propertyPhaseSchema.EnsureAsync(HttpContext.RequestAborted);
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
            await _propertyPhaseSchema.EnsureAsync(HttpContext.RequestAborted);
            var projectId = await GetValidSelectedProjectIdAsync(userId);
            if (projectId == null)
            {
                TempData["WarningMessage"] = "Please select a specific project from the top navigation to add a phase.";
                return RedirectToAction("Index", "Projects");
            }

            var result = await CreatePhaseAsync(request, projectId.Value, userId);
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
            ViewBag.ProjectProperties = await GetProjectPropertiesAsync(projectId, userId);
        }

        private async System.Threading.Tasks.Task<(bool Success, string? Message)> CreatePhaseAsync(PhaseRequest request, int projectId, int userId)
        {
            var phaseType = await _context.PhaseTypes.FirstOrDefaultAsync(p => p.PhaseTypeId == request.PhaseTypeId);
            if (phaseType == null)
                return (false, "Please select a valid phase type.");

            if (!request.PropertyId.HasValue)
                return (false, "Please select the property this phase belongs to.");

            var propertyValid = await _context.Properties.AnyAsync(p =>
                p.PropertyId == request.PropertyId.Value && p.ProjectId == projectId && p.UserId == userId);
            if (!propertyValid)
                return (false, "Please select a valid property for this project.");

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
                PropertyId = request.PropertyId,
                PhaseTypeId = request.PhaseTypeId,
                CustomPhaseName = isCustomPhase && !string.IsNullOrWhiteSpace(customName) ? customName : null,
                Sequence = sequence,
                StartDate = startDate,
                EndDate = isCompleted ? today : null,
                IsCompleted = isCompleted,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            });

            await _context.SaveChangesAsync();
            await RefreshPropertyAndProjectCompletionAsync(request.PropertyId, projectId, userId);
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

            if (!request.PropertyId.HasValue)
                return Json(new { success = false, message = "Please select the property this phase belongs to." });

            var propertyValid = await _context.Properties.AnyAsync(p =>
                p.PropertyId == request.PropertyId.Value && p.ProjectId == phase.ProjectId && p.UserId == userId);
            if (!propertyValid)
                return Json(new { success = false, message = "Please select a valid property for this project." });

            var sequence = request.Sequence <= 0 ? phase.Sequence : request.Sequence;
            var sequenceTaken = await _context.Phases.AnyAsync(p =>
                p.ProjectId == phase.ProjectId && p.Sequence == sequence && p.PhaseId != phase.PhaseId);
            if (sequenceTaken)
                return Json(new { success = false, message = "Another phase already uses this sequence number." });

            var customName = request.CustomPhaseName?.Trim();
            if (phaseType.PhaseName.Equals("Custom", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(customName))
                return Json(new { success = false, message = "Custom phase name is required." });

            var previousPropertyId = phase.PropertyId;
            var isCustomPhase = phaseType.PhaseName.Equals("Custom", StringComparison.OrdinalIgnoreCase);
            phase.PhaseTypeId = request.PhaseTypeId;
            phase.PropertyId = request.PropertyId;
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
            await RefreshPropertyAndProjectCompletionAsync(previousPropertyId, phase.ProjectId, userId);
            if (previousPropertyId != phase.PropertyId)
                await RefreshPropertyAndProjectCompletionAsync(phase.PropertyId, phase.ProjectId, userId);
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

            var projectId = phase.ProjectId;
            var propertyId = phase.PropertyId;
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
            await RefreshPropertyAndProjectCompletionAsync(propertyId, projectId, userId);
            return Json(new { success = true });
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<IActionResult> AddTask([FromBody] TaskRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await CreateTaskAsync(request, userId);
            if (!result.Allowed)
                return Forbid();
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            return Json(new { success = true });
        }

        [HttpGet]
        public async System.Threading.Tasks.Task<IActionResult> AddTask(int phaseId)
        {
            var userId = GetCurrentUserId();
            var allowed = await PopulateAddTaskPageAsync(phaseId, userId);
            if (!allowed)
                return Forbid();

            return View(new TaskRequest
            {
                PhaseId = phaseId,
                StatusId = await ResolveStatusIdAsync(null, "Pending"),
                StartDate = DateOnly.FromDateTime(DateTime.Today)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<IActionResult> AddTaskPage(TaskRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await CreateTaskAsync(request, userId);
            if (!result.Allowed)
                return Forbid();

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message ?? "Unable to add task.");
                await PopulateAddTaskPageAsync(request.PhaseId, userId);
                return View("AddTask", request);
            }

            return RedirectToAction(nameof(Index));
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

        private async System.Threading.Tasks.Task<(bool Allowed, bool Success, string? Message)> CreateTaskAsync(TaskRequest request, int userId)
        {
            if (!await PhaseBelongsToUserAsync(request.PhaseId, userId))
                return (false, false, null);

            if (string.IsNullOrWhiteSpace(request.TaskName))
                return (true, false, "Task name is required.");

            var dateError = ValidateDateRange(request.StartDate, request.EndDate, "task");
            if (dateError != null)
                return (true, false, dateError);

            var statusId = await ResolveStatusIdAsync(request.StatusId, request.Status);
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
            return (true, true, null);
        }

        private async System.Threading.Tasks.Task<bool> PopulateAddTaskPageAsync(int phaseId, int userId)
        {
            var phase = await _context.Phases
                .AsNoTracking()
                .Include(p => p.Project)
                .Include(p => p.Property)
                .Include(p => p.PhaseType)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Status)
                .FirstOrDefaultAsync(p => p.PhaseId == phaseId && p.Project.UserId == userId);
            if (phase == null)
                return false;

            ViewBag.ProjectName = phase.Project.ProjectName;
            ViewBag.PropertyName = phase.Property?.PropertyName ?? "Unassigned";
            ViewBag.PhaseName = string.IsNullOrWhiteSpace(phase.CustomPhaseName) ? phase.PhaseType.PhaseName : phase.CustomPhaseName;
            ViewBag.PhaseSequence = phase.Sequence;
            ViewBag.ExistingTasks = phase.Tasks.OrderBy(t => t.CreatedAt).ToList();
            ViewBag.TaskStatuses = await _context.TaskStatuses
                .AsNoTracking()
                .OrderBy(s => s.StatusId)
                .ToListAsync();
            return true;
        }

        private async System.Threading.Tasks.Task RefreshPhaseCompletionAsync(int phaseId)
        {
            var phase = await _context.Phases
                .Include(p => p.Project)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Status)
                .FirstOrDefaultAsync(p => p.PhaseId == phaseId);
            if (phase == null)
                return;

            if (phase.Tasks.Any())
            {
                phase.IsCompleted = phase.Tasks.All(t => IsCompleted(t.Status?.StatusName, t.StatusId));
                phase.EndDate = phase.IsCompleted
                    ? phase.EndDate ?? DateOnly.FromDateTime(DateTime.Today)
                    : null;
                await _context.SaveChangesAsync();
            }

            await RefreshPropertyAndProjectCompletionAsync(phase.PropertyId, phase.ProjectId, phase.Project.UserId);
        }

        private async System.Threading.Tasks.Task RefreshPropertyAndProjectCompletionAsync(int? propertyId, int projectId, int userId)
        {
            if (propertyId.HasValue)
            {
                var property = await _context.Properties
                    .Include(p => p.Status)
                    .FirstOrDefaultAsync(p => p.PropertyId == propertyId.Value && p.ProjectId == projectId && p.UserId == userId);

                if (property != null)
                {
                    var phases = await _context.Phases
                        .Include(p => p.Tasks)
                            .ThenInclude(t => t.Status)
                        .Where(p => p.ProjectId == projectId && p.PropertyId == property.PropertyId)
                        .ToListAsync();

                    if (phases.Any())
                    {
                        var allConstructionComplete = phases.All(PhaseIsComplete);
                        var propertyCompleted = string.Equals(property.Status.StatusName, "Completed", StringComparison.OrdinalIgnoreCase);

                        if (allConstructionComplete && !propertyCompleted)
                        {
                            var completedStatusId = await ResolvePropertyStatusIdAsync("Completed");
                            if (completedStatusId.HasValue)
                                property.StatusId = completedStatusId.Value;
                        }
                        else if (!allConstructionComplete && propertyCompleted)
                        {
                            var openStatusId = await ResolveOpenPropertyStatusIdAsync();
                            if (openStatusId.HasValue)
                                property.StatusId = openStatusId.Value;
                        }

                        if (_context.ChangeTracker.HasChanges())
                        {
                            property.UpdatedAt = DateTime.Now;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }

            await SyncProjectCompletionFromPropertiesAsync(projectId, userId);
        }

        private static bool PhaseIsComplete(Phase phase)
        {
            return phase.Tasks.Any()
                ? phase.Tasks.All(t => IsCompleted(t.Status?.StatusName, t.StatusId))
                : phase.IsCompleted;
        }

        private async System.Threading.Tasks.Task<byte?> ResolvePropertyStatusIdAsync(params string[] preferredNames)
        {
            foreach (var name in preferredNames)
            {
                var statusId = await _context.PropertyStatuses
                    .Where(s => s.StatusName == name)
                    .Select(s => (byte?)s.StatusId)
                    .FirstOrDefaultAsync();
                if (statusId.HasValue)
                    return statusId;
            }

            return null;
        }

        private async System.Threading.Tasks.Task<byte?> ResolveOpenPropertyStatusIdAsync()
        {
            var preferredStatusId = await ResolvePropertyStatusIdAsync("Under Construction", "Planned", "On Hold");
            if (preferredStatusId.HasValue)
                return preferredStatusId;

            return await _context.PropertyStatuses
                .Where(s => s.StatusName != "Completed")
                .OrderBy(s => s.StatusId)
                .Select(s => (byte?)s.StatusId)
                .FirstOrDefaultAsync();
        }

        private async System.Threading.Tasks.Task<List<Property>> GetProjectPropertiesAsync(int projectId, int userId)
        {
            return await _context.Properties
                .AsNoTracking()
                .Include(p => p.Status)
                .Where(p => p.UserId == userId && p.ProjectId == projectId)
                .OrderBy(p => p.PropertyName)
                .ToListAsync();
        }

        private async System.Threading.Tasks.Task SyncCompletedPropertyPhasesAsync(int projectId, int userId)
        {
            var completedStatusIds = await _context.PropertyStatuses
                .Where(s => s.StatusName == "Completed")
                .Select(s => s.StatusId)
                .ToListAsync();
            if (!completedStatusIds.Any())
                return;

            var completedPropertyIds = await _context.Properties
                .Where(p => p.UserId == userId && p.ProjectId == projectId && completedStatusIds.Contains(p.StatusId))
                .Select(p => p.PropertyId)
                .ToListAsync();
            if (!completedPropertyIds.Any())
                return;

            var completedTaskStatusId = await ResolveStatusIdAsync(null, "Completed");
            var phases = await _context.Phases
                .Include(p => p.Tasks)
                .Where(p => p.ProjectId == projectId && p.PropertyId.HasValue && completedPropertyIds.Contains(p.PropertyId.Value))
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);
            foreach (var phase in phases)
            {
                phase.IsCompleted = true;
                phase.EndDate ??= today;
                foreach (var task in phase.Tasks)
                {
                    if (task.StatusId != completedTaskStatusId)
                    {
                        task.StatusId = completedTaskStatusId;
                        task.UpdatedAt = DateTime.Now;
                    }
                }
            }

            if (phases.Any())
                await _context.SaveChangesAsync();

            await SyncProjectCompletionFromPropertiesAsync(projectId, userId);
        }

        private async System.Threading.Tasks.Task SyncProjectCompletionFromPropertiesAsync(int projectId, int userId)
        {
            var propertyRows = await _context.Properties
                .Where(p => p.UserId == userId && p.ProjectId == projectId)
                .Select(p => new { p.Status.StatusName })
                .ToListAsync();
            if (!propertyRows.Any())
                return;

            var allCompleted = propertyRows.All(p => string.Equals(p.StatusName, "Completed", StringComparison.OrdinalIgnoreCase));
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId && p.UserId == userId);
            if (project == null || project.IsCompleted == allCompleted)
                return;

            project.IsCompleted = allCompleted;
            project.ActualEndDate = allCompleted ? DateOnly.FromDateTime(DateTime.Today) : null;
            project.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public class PhaseRequest
        {
            public int PhaseId { get; set; }
            public int? PropertyId { get; set; }
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
