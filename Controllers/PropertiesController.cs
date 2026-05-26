using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BuildWise.Models;
using BuildWise.BusinessLayer;
using BuildWise.Services;

namespace BuildWise.Controllers
{
    [Authorize]
    public class PropertiesController : Controller
    {
        private readonly PropertyBLL _propertyBll;
        private readonly BuildWiseDbContext _context; // For dropdowns
        private readonly PropertyPhaseSchemaService _propertyPhaseSchema;

        public PropertiesController(PropertyBLL propertyBll, BuildWiseDbContext context, PropertyPhaseSchemaService propertyPhaseSchema)
        {
            _propertyBll = propertyBll;
            _context = context;
            _propertyPhaseSchema = propertyPhaseSchema;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue("UserId") ?? "0");
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var properties = await _propertyBll.GetUserPropertiesAsync(userId);
            return View(properties);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            var property = await _propertyBll.GetPropertyDetailsAsync(id, userId);
            if (property == null) return NotFound();

            return View(property);
        }

        public IActionResult Create(int? projectId)
        {
            var property = new Property { ProjectId = projectId };
            PopulateDropdowns(property);
            ViewBag.SourceProjectId = projectId;
            return View(property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PropertyName,ProjectId,TypeId,StatusId,Location,City,AreaSize,AreaUnitId,Notes")] Property property)
        {
            var userId = GetCurrentUserId();
            property.UserId = userId;
            
            // Remove navigation properties from model state validation
            ModelState.Remove("User");
            ModelState.Remove("Project");
            ModelState.Remove("Type");
            ModelState.Remove("Status");
            ModelState.Remove("AreaUnit");
            ModelState.Remove("Projects");
            ModelState.Remove("Phases");

            await ValidateProjectSelectionAsync(property.ProjectId, userId);

            if (ModelState.IsValid)
            {
                try
                {
                    await _propertyBll.AddPropertyAsync(property);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            PopulateDropdowns(property);
            ViewBag.SourceProjectId = property.ProjectId;
            return View(property);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            var property = await _propertyBll.GetPropertyDetailsAsync(id, userId);
            if (property == null) return NotFound();

            PopulateDropdowns(property);
            return View(property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PropertyId,PropertyName,ProjectId,TypeId,StatusId,Location,City,AreaSize,AreaUnitId,Notes")] Property property)
        {
            if (id != property.PropertyId) return NotFound();

            var userId = GetCurrentUserId();

            ModelState.Remove("User");
            ModelState.Remove("Project");
            ModelState.Remove("Type");
            ModelState.Remove("Status");
            ModelState.Remove("AreaUnit");
            ModelState.Remove("Projects");
            ModelState.Remove("Phases");

            await ValidateProjectSelectionAsync(property.ProjectId, userId);

            if (ModelState.IsValid)
            {
                try
                {
                    await _propertyBll.UpdatePropertyAsync(property, userId);
                    await SyncConstructionAfterPropertyStatusChangeAsync(property.PropertyId, userId);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            PopulateDropdowns(property);
            return View(property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            await _propertyBll.DeletePropertyAsync(id, userId);
            return RedirectToAction(nameof(Index));
        }

        private void PopulateDropdowns(Property? property = null)
        {
            var userId = GetCurrentUserId();
            ViewData["ProjectId"] = new SelectList(
                _context.Set<Project>()
                    .AsNoTracking()
                    .Where(p => p.UserId == userId)
                    .OrderBy(p => p.ProjectName),
                "ProjectId",
                "ProjectName",
                property?.ProjectId);
            ViewData["TypeId"] = new SelectList(_context.Set<PropertyType>().AsNoTracking(), "TypeId", "TypeName", property?.TypeId);
            ViewData["StatusId"] = new SelectList(_context.Set<PropertyStatus>().AsNoTracking(), "StatusId", "StatusName", property?.StatusId);
            ViewData["AreaUnitId"] = new SelectList(_context.Set<AreaUnit>().AsNoTracking(), "UnitId", "UnitName", property?.AreaUnitId);
        }

        private async System.Threading.Tasks.Task ValidateProjectSelectionAsync(int? projectId, int userId)
        {
            if (!projectId.HasValue) return;

            var ownsProject = await _context.Projects
                .AsNoTracking()
                .AnyAsync(p => p.ProjectId == projectId.Value && p.UserId == userId);

            if (!ownsProject)
            {
                ModelState.AddModelError(nameof(Property.ProjectId), "Select a valid project.");
            }
        }

        private async System.Threading.Tasks.Task SyncConstructionAfterPropertyStatusChangeAsync(int propertyId, int userId)
        {
            await _propertyPhaseSchema.EnsureAsync(HttpContext.RequestAborted);

            var property = await _context.Properties
                .Include(p => p.Status)
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.UserId == userId);
            if (property?.ProjectId == null)
                return;

            if (string.Equals(property.Status.StatusName, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                var completedTaskStatusId = await _context.TaskStatuses
                    .Where(s => s.StatusName == "Completed")
                    .Select(s => (byte?)s.StatusId)
                    .FirstOrDefaultAsync() ?? 3;
                var phases = await _context.Phases
                    .Include(p => p.Tasks)
                    .Where(p => p.PropertyId == propertyId && p.ProjectId == property.ProjectId.Value)
                    .ToListAsync();
                var today = DateOnly.FromDateTime(DateTime.Today);
                foreach (var phase in phases)
                {
                    phase.IsCompleted = true;
                    phase.EndDate ??= today;
                    foreach (var task in phase.Tasks)
                    {
                        task.StatusId = completedTaskStatusId;
                        task.UpdatedAt = DateTime.Now;
                    }
                }
            }

            var propertyStatuses = await _context.Properties
                .Where(p => p.UserId == userId && p.ProjectId == property.ProjectId.Value)
                .Select(p => p.Status.StatusName)
                .ToListAsync();
            var allCompleted = propertyStatuses.Any() && propertyStatuses.All(s => string.Equals(s, "Completed", StringComparison.OrdinalIgnoreCase));
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == property.ProjectId.Value && p.UserId == userId);
            if (project != null)
            {
                project.IsCompleted = allCompleted;
                project.ActualEndDate = allCompleted ? DateOnly.FromDateTime(DateTime.Today) : null;
                project.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }
    }
}
