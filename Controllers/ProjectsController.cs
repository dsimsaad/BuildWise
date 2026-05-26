using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BuildWise.Models;
using BuildWise.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace BuildWise.Controllers
{
    [Authorize]
    public class ProjectsController : BaseController
    {
        private readonly BuildWiseDbContext _context;
        private readonly IMemoryCache _cache;

        public ProjectsController(BuildWiseDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        private void ClearProjectSelectorCache(int userId)
        {
            _cache.Remove(ProjectCacheKeys.SelectorProjects(userId));
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            int userId = GetUserId();
            var projects = await _context.Projects
                .Where(p => p.UserId == userId)
                .ToListAsync();
            return View(projects);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            int userId = GetUserId();

            var project = await _context.Projects
                .Include(p => p.Property)
                .Include(p => p.Properties)
                    .ThenInclude(p => p.Status)
                .Include(p => p.Properties)
                    .ThenInclude(p => p.Type)
                .Include(p => p.Properties)
                    .ThenInclude(p => p.AreaUnit)
                .Include(p => p.Budget)
                .Include(p => p.Expenses)
                .Include(p => p.Phases)
                    .ThenInclude(p => p.Tasks)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.ProjectId == id && m.UserId == userId);

            if (project == null) return NotFound();
            return View(project);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {
            return View(new ProjectCreateViewModel());
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectCreateViewModel model)
        {
            int currentUserId = GetUserId();

            if (ModelState.IsValid)
            {
                var defaultTypeId = await _context.PropertyTypes
                    .OrderBy(t => t.TypeId)
                    .Select(t => (byte?)t.TypeId)
                    .FirstOrDefaultAsync();
                var defaultStatusId = await _context.PropertyStatuses
                    .OrderBy(s => s.StatusId)
                    .Select(s => (byte?)s.StatusId)
                    .FirstOrDefaultAsync();
                var defaultAreaUnitId = await _context.AreaUnits
                    .OrderBy(a => a.UnitId)
                    .Select(a => (byte?)a.UnitId)
                    .FirstOrDefaultAsync();

                if (!defaultTypeId.HasValue || !defaultStatusId.HasValue || !defaultAreaUnitId.HasValue)
                {
                    ModelState.AddModelError("", "Unable to initialize required default property metadata.");
                    return View(model);
                }

                var bootstrapProperty = new Property
                {
                    UserId = currentUserId,
                    PropertyName = $"{model.ProjectName.Trim()} Property",
                    TypeId = defaultTypeId.Value,
                    StatusId = defaultStatusId.Value,
                    AreaUnitId = defaultAreaUnitId.Value,
                    AreaSize = 0,
                    Location = "Not specified",
                    City = null,
                    Notes = "Auto-created for quick project setup.",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Properties.Add(bootstrapProperty);
                await _context.SaveChangesAsync();

                var project = new Project
                {
                    ProjectName = model.ProjectName.Trim(),
                    Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                    PropertyId = bootstrapProperty.PropertyId,
                    UserId = currentUserId,
                    StartDate = DateOnly.FromDateTime(DateTime.Now),
                    ExpectedEndDate = null,
                    ActualEndDate = null,
                    TotalBudget = 0,
                    IsCompleted = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Add(project);
                await _context.SaveChangesAsync();

                bootstrapProperty.ProjectId = project.ProjectId;
                bootstrapProperty.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                ClearProjectSelectorCache(currentUserId);
                HttpContext.Session.SetInt32("SelectedProjectId", project.ProjectId);
                return RedirectToAction("Dashboard", "Home");
            }
            return View(model);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            int userId = GetUserId();

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == id && p.UserId == userId);
            if (project == null) return NotFound();

            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectId,ProjectName,Description,IsCompleted")] Project project)
        {
            if (id != project.ProjectId) return NotFound();
            int userId = GetUserId();

            if (string.IsNullOrWhiteSpace(project.ProjectName))
            {
                ModelState.AddModelError(nameof(Project.ProjectName), "Project name is required.");
            }

            RemoveModelStateEntries(
                nameof(Project.Property),
                nameof(Project.User),
                nameof(Project.PropertyId),
                nameof(Project.UserId),
                nameof(Project.StartDate),
                nameof(Project.CreatedAt),
                nameof(Project.UpdatedAt));

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProject = await _context.Projects
                        .FirstOrDefaultAsync(p => p.ProjectId == id && p.UserId == userId);
                    if (existingProject == null) return NotFound();

                    existingProject.ProjectName = project.ProjectName.Trim();
                    existingProject.Description = string.IsNullOrWhiteSpace(project.Description) ? null : project.Description.Trim();
                    existingProject.IsCompleted = project.IsCompleted;
                    existingProject.ActualEndDate = project.IsCompleted && existingProject.ActualEndDate == null
                        ? DateOnly.FromDateTime(DateTime.Today)
                        : project.IsCompleted ? existingProject.ActualEndDate : null;
                    existingProject.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    ClearProjectSelectorCache(userId);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.ProjectId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            int userId = GetUserId();

            var project = await _context.Projects
                .Include(p => p.Property)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.ProjectId == id && m.UserId == userId);

            if (project == null) return NotFound();
            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            int userId = GetUserId();
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == id && p.UserId == userId);
            if (project != null)
            {
                _context.Projects.Remove(project);
            }

            await _context.SaveChangesAsync();
            ClearProjectSelectorCache(userId);
            if (HttpContext.Session.GetInt32("SelectedProjectId") == id)
            {
                var nextProjectId = await _context.Projects
                    .Where(p => p.UserId == userId)
                    .OrderBy(p => p.ProjectId)
                    .Select(p => (int?)p.ProjectId)
                    .FirstOrDefaultAsync();

                if (nextProjectId.HasValue)
                {
                    HttpContext.Session.SetInt32("SelectedProjectId", nextProjectId.Value);
                }
                else
                {
                    HttpContext.Session.Remove("SelectedProjectId");
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id) =>
            _context.Projects.Any(e => e.ProjectId == id && e.UserId == GetUserId());
    }
}
