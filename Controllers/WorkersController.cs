using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BuildWise.Models;

namespace BuildWise.Controllers
{
    public class WorkersController : Controller
    {
        private readonly BuildWiseDbContext _context;

        public WorkersController(BuildWiseDbContext context)
        {
            _context = context;
        }

        // GET: Workers
        public async Task<IActionResult> Index()
        {
            var workers = await _context.Workers
                .Include(w => w.Contractor)
                .ToListAsync();
            return View(workers);
        }

        // GET: Workers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var worker = await _context.Workers
                .Include(w => w.Contractor)
                .FirstOrDefaultAsync(m => m.WorkerId == id);

            if (worker == null) return NotFound();
            return View(worker);
        }

        // GET: Workers/Create
        public IActionResult Create()
        {
            ViewData["ContractorId"] = new SelectList(_context.Contractors, "ContractorId", "FullName");
            return View();
        }

        // POST: Workers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("WorkerId,ContractorId,FullName,Phone,Cnic,DailyWage,SkillType,IsActive")] Worker worker)
        {
            if (ModelState.IsValid)
            {
                worker.CreatedAt = DateTime.Now;
                worker.IsActive = true;
                _context.Add(worker);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ContractorId"] = new SelectList(_context.Contractors, "ContractorId", "FullName", worker.ContractorId);
            return View(worker);
        }

        // GET: Workers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var worker = await _context.Workers.FindAsync(id);
            if (worker == null) return NotFound();

            ViewData["ContractorId"] = new SelectList(_context.Contractors, "ContractorId", "FullName", worker.ContractorId);
            return View(worker);
        }

        // POST: Workers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("WorkerId,ContractorId,FullName,Phone,Cnic,DailyWage,SkillType,IsActive,CreatedAt")] Worker worker)
        {
            if (id != worker.WorkerId) return NotFound();

            if (ModelState.IsValid)
            {
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
            ViewData["ContractorId"] = new SelectList(_context.Contractors, "ContractorId", "FullName", worker.ContractorId);
            return View(worker);
        }

        // GET: Workers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var worker = await _context.Workers
                .Include(w => w.Contractor)
                .FirstOrDefaultAsync(m => m.WorkerId == id);

            if (worker == null) return NotFound();
            return View(worker);
        }

        // POST: Workers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var worker = await _context.Workers.FindAsync(id);
            if (worker != null)
                _context.Workers.Remove(worker);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool WorkerExists(int id) =>
            _context.Workers.Any(e => e.WorkerId == id);
    }
}
