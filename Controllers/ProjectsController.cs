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

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects
                .ToListAsync();
            return View(projects);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.Property)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.ProjectId == id);

            if (project == null) return NotFound();
            return View(project);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectName,Description")] Project project)
        {
            var userIdClaim = User.FindFirstValue("UserId");
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                ModelState.AddModelError("", "Unable to identify current user.");
            }

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
                        return View(project);
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

                project.PropertyId = propertyId.Value;
                project.UserId = currentUserId;
                project.StartDate = DateOnly.FromDateTime(DateTime.Now);
                project.ExpectedEndDate = null;
                project.ActualEndDate = null;
                project.TotalBudget = 0;
                project.IsCompleted = false;
                project.CreatedAt = DateTime.Now;
                project.UpdatedAt = DateTime.Now;
                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectId,ProjectName,Description,IsCompleted,CreatedAt")] Project project)
        {
            if (id != project.ProjectId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProject = await _context.Projects.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ProjectId == id);
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

            var project = await _context.Projects
                .Include(p => p.Property)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.ProjectId == id);

            if (project == null) return NotFound();
            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project != null)
                _context.Projects.Remove(project);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id) =>
            _context.Projects.Any(e => e.ProjectId == id);
    }
}
