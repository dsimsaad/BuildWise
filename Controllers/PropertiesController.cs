using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BuildWise.Models;
using BuildWise.BusinessLayer;

namespace BuildWise.Controllers
{
    [Authorize]
    public class PropertiesController : Controller
    {
        private readonly PropertyBLL _propertyBll;
        private readonly BuildWiseDbContext _context; // For dropdowns

        public PropertiesController(PropertyBLL propertyBll, BuildWiseDbContext context)
        {
            _propertyBll = propertyBll;
            _context = context;
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

            await ValidateProjectSelectionAsync(property.ProjectId, userId);

            if (ModelState.IsValid)
            {
                try
                {
                    await _propertyBll.UpdatePropertyAsync(property, userId);
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
    }
}
