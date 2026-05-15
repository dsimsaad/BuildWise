using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BuildWise.Models;
using BuildWise.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BuildWise.Controllers
{
    [Authorize]
    public class WorkersController : Controller
    {
        private readonly BuildWiseDbContext _context;
        private readonly ExpenseBLL _expenseBll;
        private static readonly string[] CommonSkills = { "Mason", "Electrician", "Plumber", "Helper" };

        public WorkersController(BuildWiseDbContext context, IConfiguration configuration)
        {
            _context = context;
            _expenseBll = new ExpenseBLL(configuration.GetConnectionString("BuildWise") ?? "");
        }

        private int GetUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        // GET: Workers
        public async Task<IActionResult> Index(string? skill)
        {
            int userId = GetUserId();
            var selectedProjectId = HttpContext.Session.GetInt32("SelectedProjectId");

            var workersQuery = GetScopedWorkersQuery(userId, selectedProjectId);

            var availableSkills = await workersQuery
                .Where(w => !string.IsNullOrWhiteSpace(w.SkillType))
                .Select(w => w.SkillType!)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(skill))
            {
                workersQuery = workersQuery.Where(w => w.SkillType == skill);
            }

            ViewBag.SkillFilter = skill;
            ViewBag.SkillOptions = availableSkills;
            ViewBag.SelectedProjectId = selectedProjectId;
            ViewBag.TodayWagesRecorded = selectedProjectId.HasValue && await HasRecordedWagesForToday(userId, selectedProjectId.Value);
            ViewBag.WorkerTotals = await _context.WagePayments
                .Where(wp => wp.Worker.UserId == userId || wp.Worker.UserId == null)
                .GroupBy(wp => wp.WorkerId)
                .Select(g => new { WorkerId = g.Key, Total = g.Sum(wp => wp.AmountPaid) })
                .ToDictionaryAsync(x => x.WorkerId, x => x.Total);

            var workers = await workersQuery
                .OrderByDescending(w => w.DailyWage)
                .ToListAsync();
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
            ViewBag.CommonSkills = CommonSkills;
            return View();
        }

        // POST: Workers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("WorkerId,FullName,Phone,Cnic,DailyWage")] Worker worker, string? SkillChoice, string? CustomSkill)
        {
            int userId = GetUserId();
            ModelState.Remove(nameof(worker.DailyWage));

            worker.SkillType = SkillChoice == "Custom" ? CustomSkill?.Trim() : SkillChoice?.Trim();
            worker.FullName = worker.FullName?.Trim() ?? string.Empty;
            worker.ContractorId = null;

            ApplyWorkerContactRules(worker, userId);

            if (string.IsNullOrWhiteSpace(worker.FullName))
            {
                ModelState.AddModelError(nameof(worker.FullName), "Worker name is required.");
            }

            if (string.IsNullOrWhiteSpace(worker.SkillType))
            {
                ModelState.AddModelError(nameof(worker.SkillType), "Skill type is required.");
            }

            if (ModelState.IsValid)
            {
                worker.UserId = userId;
                worker.CreatedAt = DateTime.Now;
                worker.IsActive = true;
                try
                {
                    _context.Add(worker);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(nameof(worker.Cnic), "A worker with this CNIC already exists.");
                }
            }

            ViewBag.CommonSkills = CommonSkills;
            ViewBag.SelectedSkillChoice = SkillChoice;
            ViewBag.CustomSkill = CustomSkill;
            return View(worker);
        }

        // POST: Workers/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, string? skill)
        {
            int userId = GetUserId();
            var worker = await _context.Workers.FirstOrDefaultAsync(w => w.WorkerId == id && (w.UserId == userId || w.UserId == null));

            if (worker == null) return NotFound();

            worker.IsActive = !worker.IsActive;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { skill });
        }

        // POST: Workers/SetAllStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAllStatus(bool active, string? skill)
        {
            int userId = GetUserId();
            var selectedProjectId = HttpContext.Session.GetInt32("SelectedProjectId");
            var workers = await GetScopedWorkersQuery(userId, selectedProjectId)
                .Where(w => string.IsNullOrWhiteSpace(skill) || w.SkillType == skill)
                .ToListAsync();

            foreach (var worker in workers)
            {
                worker.IsActive = active;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { skill });
        }

        // POST: Workers/RecordDailyWages
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordDailyWages(IFormCollection form, string? skill)
        {
            int userId = GetUserId();
            var selectedProjectId = HttpContext.Session.GetInt32("SelectedProjectId");
            if (!selectedProjectId.HasValue)
            {
                TempData["WorkersMessage"] = "Select a project before recording daily wages.";
                return RedirectToAction(nameof(Index), new { skill });
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (await HasRecordedWagesForToday(userId, selectedProjectId.Value))
            {
                TempData["WorkersMessage"] = "Today's labour wages are already recorded for this project.";
                return RedirectToAction(nameof(Index), new { skill });
            }

            var activeWorkers = await GetScopedWorkersQuery(userId, selectedProjectId)
                .Where(w => w.IsActive && (string.IsNullOrWhiteSpace(skill) || w.SkillType == skill))
                .ToListAsync();

            if (!activeWorkers.Any())
            {
                TempData["WorkersMessage"] = "No active workers found for today's wage recording.";
                return RedirectToAction(nameof(Index), new { skill });
            }

            decimal totalDailyWages = 0;
            foreach (var worker in activeWorkers)
            {
                var bonus = ParseBonus(form[$"bonuses[{worker.WorkerId}]"]);
                var amountPaid = worker.DailyWage + bonus;
                if (amountPaid <= 0) continue;

                var notes = bonus > 0
                    ? $"Daily wage {worker.DailyWage:N0} + bonus {bonus:N0}"
                    : $"Daily wage {worker.DailyWage:N0}";

                _context.WagePayments.Add(new WagePayment
                {
                    WorkerId = worker.WorkerId,
                    ProjectId = selectedProjectId.Value,
                    AmountPaid = amountPaid,
                    PaymentDate = today,
                    PeriodFrom = today,
                    PeriodTo = today,
                    Notes = notes,
                    CreatedAt = DateTime.Now
                });

                totalDailyWages += amountPaid;
            }

            if (totalDailyWages <= 0)
            {
                TempData["WorkersMessage"] = "No payable wages found for today's active workers.";
                return RedirectToAction(nameof(Index), new { skill });
            }

            var expenseDate = DateTime.Today;
            var description = $"Daily worker wages for {expenseDate:yyyy-MM-dd}";
            var expenseItem = new ExpenseItem
            {
                ProjectId = selectedProjectId.Value,
                Category = "Labour",
                Description = description,
                Amount = totalDailyWages,
                ExpenseDate = expenseDate
            };

            var expenseSaved = _expenseBll.AddExpense(expenseItem);
            if (expenseSaved)
            {
                await _context.SaveChangesAsync();
            }

            TempData["WorkersMessage"] = expenseSaved
                ? $"Recorded today's worker wages: PKR {totalDailyWages.ToString("N0", new CultureInfo("en-IN"))}."
                : "Wage payments were saved, but the Labour expense could not be recorded.";

            return RedirectToAction(nameof(Index), new { skill });
        }

        // POST: Workers/UndoDailyWages
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UndoDailyWages(string? skill)
        {
            int userId = GetUserId();
            var selectedProjectId = HttpContext.Session.GetInt32("SelectedProjectId");
            if (!selectedProjectId.HasValue)
            {
                TempData["WorkersMessage"] = "Select a project before undoing daily wages.";
                return RedirectToAction(nameof(Index), new { skill });
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var todaysPayments = await _context.WagePayments
                .Where(wp =>
                    wp.ProjectId == selectedProjectId.Value &&
                    wp.Project.UserId == userId &&
                    wp.PaymentDate == today &&
                    wp.PeriodFrom == today &&
                    wp.PeriodTo == today)
                .ToListAsync();

            if (!todaysPayments.Any())
            {
                TempData["WorkersMessage"] = "No wage record found for today.";
                return RedirectToAction(nameof(Index), new { skill });
            }

            _context.WagePayments.RemoveRange(todaysPayments);
            await _context.SaveChangesAsync();

            var expenseDate = DateTime.Today;
            var description = $"Daily worker wages for {expenseDate:yyyy-MM-dd}";
            var existingExpense = _expenseBll
                .GetAllExpenses(selectedProjectId.Value, userId)
                .FirstOrDefault(e =>
                    e.Category == "Labour" &&
                    e.Description == description &&
                    e.ExpenseDate.Date == expenseDate);

            if (existingExpense != null)
            {
                _expenseBll.DeleteExpense(existingExpense.ExpenseId);
            }

            TempData["WorkersMessage"] = "Today's wage record has been undone.";
            return RedirectToAction(nameof(Index), new { skill });
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
            ApplyWorkerContactRules(worker, userId, worker.WorkerId);

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
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(nameof(worker.Cnic), "A worker with this CNIC already exists.");
                    ViewData["ContractorId"] = new SelectList(_context.Contractors.Where(c => c.UserId == userId || c.UserId == null), "ContractorId", "FullName", worker.ContractorId);
                    return View(worker);
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

        private IQueryable<Worker> GetScopedWorkersQuery(int userId, int? selectedProjectId)
        {
            return _context.Workers
                .Where(w => w.UserId == userId || w.UserId == null)
                .AsQueryable();
        }

        private static decimal ParseBonus(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue)) return 0;
            return decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value > 0
                ? value
                : 0;
        }

        private async Task<bool> HasRecordedWagesForToday(int userId, int projectId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var expenseDate = DateTime.Today;
            var description = $"Daily worker wages for {expenseDate:yyyy-MM-dd}";

            if (await _context.WagePayments.AnyAsync(wp =>
                wp.ProjectId == projectId &&
                wp.Project.UserId == userId &&
                wp.PaymentDate == today &&
                wp.PeriodFrom == today &&
                wp.PeriodTo == today))
            {
                return true;
            }

            return _expenseBll.GetAllExpenses(projectId, userId).Any(e =>
                e.Category == "Labour" &&
                e.Description == description &&
                e.ExpenseDate.Date == expenseDate);
        }

        private void ApplyWorkerContactRules(Worker worker, int userId, int? currentWorkerId = null)
        {
            worker.Phone = NormalizePhone(worker.Phone);
            worker.Cnic = NormalizeCnic(worker.Cnic);

            if (!string.IsNullOrWhiteSpace(worker.Phone) && !IsValidFormattedPhone(worker.Phone))
            {
                ModelState.AddModelError(nameof(worker.Phone), "Phone number must be exactly 11 digits. Format: 0000-0000000.");
            }

            if (!string.IsNullOrWhiteSpace(worker.Cnic) && !IsValidFormattedCnic(worker.Cnic))
            {
                ModelState.AddModelError(nameof(worker.Cnic), "CNIC must be exactly 13 digits. Format: 00000-0000000-0.");
            }

            if (!string.IsNullOrWhiteSpace(worker.Phone) && IsDuplicatePhone(worker.Phone, userId, currentWorkerId))
            {
                ModelState.AddModelError(nameof(worker.Phone), "A worker with this phone number already exists.");
            }

            if (!string.IsNullOrWhiteSpace(worker.Cnic) && IsDuplicateCnic(worker.Cnic, userId, currentWorkerId))
            {
                ModelState.AddModelError(nameof(worker.Cnic), "A worker with this CNIC already exists.");
            }
        }

        private bool IsDuplicatePhone(string phone, int userId, int? currentWorkerId) =>
            _context.Workers.Any(w =>
                w.Phone == phone &&
                (w.UserId == userId || w.UserId == null) &&
                (!currentWorkerId.HasValue || w.WorkerId != currentWorkerId.Value));

        private bool IsDuplicateCnic(string cnic, int userId, int? currentWorkerId) =>
            _context.Workers.Any(w =>
                w.Cnic == cnic &&
                (w.UserId == userId || w.UserId == null) &&
                (!currentWorkerId.HasValue || w.WorkerId != currentWorkerId.Value));

        private static string? NormalizePhone(string? value)
        {
            var digits = OnlyDigits(value);
            if (string.IsNullOrWhiteSpace(digits)) return null;
            return digits.Length == 11 ? $"{digits[..4]}-{digits[4..]}" : digits;
        }

        private static string? NormalizeCnic(string? value)
        {
            var digits = OnlyDigits(value);
            if (string.IsNullOrWhiteSpace(digits)) return null;
            return digits.Length == 13 ? $"{digits[..5]}-{digits.Substring(5, 7)}-{digits[12]}" : digits;
        }

        private static string OnlyDigits(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "" : Regex.Replace(value, @"\D", "");

        private static bool IsValidFormattedPhone(string value) =>
            Regex.IsMatch(value, @"^\d{4}-\d{7}$");

        private static bool IsValidFormattedCnic(string value) =>
            Regex.IsMatch(value, @"^\d{5}-\d{7}-\d$");
    }
}
