using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BuildWise.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BuildWise.Controllers
{
    [Authorize]
    public class WorkersController : Controller
    {
        private readonly BuildWiseDbContext _context;

        public WorkersController(BuildWiseDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        // GET: Workers
        public async Task<IActionResult> Index()
        {
            int userId = GetUserId();
            var selectedProjectId = HttpContext.Session.GetInt32("SelectedProjectId");

            var workersQuery = _context.Workers
                .Include(w => w.Contractor)
                .Where(w => w.UserId == userId || w.UserId == null) // Show shared or own
                .AsQueryable();

            if (selectedProjectId.HasValue)
            {
                workersQuery = workersQuery.Where(w =>
                    _context.TaskWorkers.Any(tw => tw.WorkerId == w.WorkerId && tw.Task.Phase.ProjectId == selectedProjectId.Value) ||
                    _context.Attendances.Any(a => a.WorkerId == w.WorkerId && a.ProjectId == selectedProjectId.Value));
            }

            var workers = await workersQuery.ToListAsync();
            return View(workers);
        }

        // GET: Workers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            int userId = GetUserId();

            var worker = await _context.Workers
                .Include(w => w.Contractor)
                .FirstOrDefaultAsync(m => m.WorkerId == id && (m.UserId == userId || m.UserId == null));

            if (worker == null) return NotFound();
            return View(worker);
        }

        // GET: Workers/Create
        public IActionResult Create()
        {
            int userId = GetUserId();
            ViewData["ContractorId"] = new SelectList(_context.Contractors.Where(c => c.UserId == userId || c.UserId == null), "ContractorId", "FullName");
            return View();
        }

        // POST: Workers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("WorkerId,ContractorId,FullName,Phone,Cnic,DailyWage,SkillType,IsActive")] Worker worker)
        {
            int userId = GetUserId();
            if (ModelState.IsValid)
            {
                worker.UserId = userId;
                worker.CreatedAt = DateTime.Now;
                worker.IsActive = true;
                _context.Add(worker);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ContractorId"] = new SelectList(_context.Contractors.Where(c => c.UserId == userId || c.UserId == null), "ContractorId", "FullName", worker.ContractorId);
            return View(worker);
        }

        // GET: Workers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            int userId = GetUserId();

            var worker = await _context.Workers.FirstOrDefaultAsync(w => w.WorkerId == id && (w.UserId == userId || w.UserId == null));
            if (worker == null) return NotFound();

            ViewData["ContractorId"] = new SelectList(_context.Contractors.Where(c => c.UserId == userId || c.UserId == null), "ContractorId", "FullName", worker.ContractorId);
            return View(worker);
        }

        // POST: Workers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("WorkerId,ContractorId,FullName,Phone,Cnic,DailyWage,SkillType,IsActive,CreatedAt,UserId")] Worker worker)
        {
            if (id != worker.WorkerId) return NotFound();
            int userId = GetUserId();

            if (ModelState.IsValid)
            {
                if (worker.UserId != userId && worker.UserId != null) return Unauthorized();

                try
                {
                    _context.Update(worker);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkerExists(worker.WorkerId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ContractorId"] = new SelectList(_context.Contractors.Where(c => c.UserId == userId || c.UserId == null), "ContractorId", "FullName", worker.ContractorId);
            return View(worker);
        }

        // GET: Workers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            int userId = GetUserId();

            var worker = await _context.Workers
                .Include(w => w.Contractor)
                .FirstOrDefaultAsync(m => m.WorkerId == id && (m.UserId == userId || m.UserId == null));

            if (worker == null) return NotFound();
            return View(worker);
        }

        // POST: Workers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            int userId = GetUserId();
            var worker = await _context.Workers.FirstOrDefaultAsync(w => w.WorkerId == id && (w.UserId == userId || w.UserId == null));
            if (worker != null)
                _context.Workers.Remove(worker);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool WorkerExists(int id) =>
            _context.Workers.Any(e => e.WorkerId == id && (e.UserId == GetUserId() || e.UserId == null));
    }
}
