using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BuildWise.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace BuildWise.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly BuildWiseDbContext _context;

        public ProjectsController(BuildWiseDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
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
                // Reuse a user's existing property if available, otherwise create a minimal default one.
                var propertyId = await _context.Properties
                    .Where(p => p.UserId == currentUserId)
                    .OrderByDescending(p => p.PropertyId)
                    .Select(p => (int?)p.PropertyId)
                    .FirstOrDefaultAsync();

                if (!propertyId.HasValue)
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
                        PropertyName = "Default Property",
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
                    propertyId = bootstrapProperty.PropertyId;
                }

                var project = new Project
                {
                    ProjectName = model.ProjectName.Trim(),
                    Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                    PropertyId = propertyId.Value,
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
                return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> Edit(int id, [Bind("ProjectId,ProjectName,Description,IsCompleted,CreatedAt")] Project project)
        {
            if (id != project.ProjectId) return NotFound();
            int userId = GetUserId();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProject = await _context.Projects.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ProjectId == id && p.UserId == userId);
                    if (existingProject == null) return NotFound();

                    project.PropertyId = existingProject.PropertyId;
                    project.UserId = existingProject.UserId;
                    project.StartDate = existingProject.StartDate;
                    project.ExpectedEndDate = existingProject.ExpectedEndDate;
                    project.ActualEndDate = existingProject.ActualEndDate;
                    project.TotalBudget = existingProject.TotalBudget;
                    project.UpdatedAt = DateTime.Now;
                    _context.Update(project);
                    await _context.SaveChangesAsync();
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
                _context.Projects.Remove(project);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id) =>
            _context.Projects.Any(e => e.ProjectId == id && e.UserId == GetUserId());
    }
}
