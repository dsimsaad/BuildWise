using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BuildWise.Models;

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
                .Include(p => p.Property)
                .Include(p => p.User)
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
            ViewData["PropertyId"] = new SelectList(_context.Properties, "PropertyId", "PropertyName");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName");
            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProjectId,PropertyId,UserId,ProjectName,Description,StartDate,ExpectedEndDate,TotalBudget,IsCompleted")] Project project)
        {
            if (ModelState.IsValid)
            {
                project.CreatedAt = DateTime.Now;
                project.UpdatedAt = DateTime.Now;
                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PropertyId"] = new SelectList(_context.Properties, "PropertyId", "PropertyName", project.PropertyId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", project.UserId);
            return View(project);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            ViewData["PropertyId"] = new SelectList(_context.Properties, "PropertyId", "PropertyName", project.PropertyId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", project.UserId);
            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectId,PropertyId,UserId,ProjectName,Description,StartDate,ExpectedEndDate,ActualEndDate,TotalBudget,IsCompleted,CreatedAt")] Project project)
        {
            if (id != project.ProjectId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
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
            ViewData["PropertyId"] = new SelectList(_context.Properties, "PropertyId", "PropertyName", project.PropertyId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", project.UserId);
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
