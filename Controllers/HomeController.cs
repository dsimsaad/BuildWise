using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BuildWise.Models;
using BuildWise.BusinessLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BuildWise.Services;

namespace BuildWise.Controllers;

public class HomeController : Controller
{
    private readonly BuildWiseDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly WorkerProjectSchemaService _workerProjectSchema;

    public HomeController(BuildWiseDbContext context, IConfiguration configuration, WorkerProjectSchemaService workerProjectSchema)
    {
        _context = context;
        _configuration = configuration;
        _workerProjectSchema = workerProjectSchema;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Dashboard");
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult Features()
    {
        return View("features");
    }

    public IActionResult Pricing()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult FAQ()
    {
        return View("faq");
    }

    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Dashboard");
        }
        return View();
    }

    public IActionResult Signup()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Dashboard");
        }
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Dashboard(bool overall = false)
    {
        await _workerProjectSchema.EnsureAsync(HttpContext.RequestAborted);

        // 1. Get current User ID from claims
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null) return RedirectToAction("Index", "Account");
        int userId = int.Parse(userIdClaim.Value);

        // 2. Fetch Selected Project and Verify Ownership
        int? selectedProjectId = overall ? null : HttpContext.Session.GetInt32("SelectedProjectId");
        if (overall)
        {
            HttpContext.Session.Remove("SelectedProjectId");
        }
        if (selectedProjectId.HasValue)
        {
            bool ownsProject = await _context.Projects.AnyAsync(p => p.ProjectId == selectedProjectId && p.UserId == userId);
            if (!ownsProject)
            {
                HttpContext.Session.Remove("SelectedProjectId");
                selectedProjectId = null;
            }
        }

        if (!overall && !selectedProjectId.HasValue)
        {
            selectedProjectId = await _context.Projects
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.ProjectName == "main" ? 0 : 1)
                .ThenBy(p => p.ProjectName)
                .Select(p => (int?)p.ProjectId)
                .FirstOrDefaultAsync();

            if (selectedProjectId.HasValue)
            {
                HttpContext.Session.SetInt32("SelectedProjectId", selectedProjectId.Value);
            }
        }
        ViewBag.SelectedProjectId = selectedProjectId;

        // 3. Base Queries Filtered by User
        var projectQuery = _context.Projects.AsNoTracking().Where(p => p.UserId == userId);
        var taskQuery    = _context.Tasks.AsNoTracking().Where(t => t.Phase.Project.UserId == userId);
        var vwDashQuery  = from v in _context.VwProjectDashboards.AsNoTracking()
                          join p in _context.Projects.AsNoTracking() on v.ProjectId equals p.ProjectId
                          where p.UserId == userId
                          select v;
        var scopedTaskQuery = selectedProjectId.HasValue
            ? taskQuery.Where(t => t.Phase.ProjectId == selectedProjectId.Value)
            : taskQuery;

        var connectionString = _configuration.GetConnectionString("BuildWise") ?? "";
        var budgetBll = new BudgetBLL(connectionString);
        var expenseBll = new ExpenseBLL(connectionString);
        var transactionBll = new TransactionBLL(connectionString);
        var advisorBll = new AdvisorBLL(connectionString);

        // 4. Populate Stats
        if (selectedProjectId.HasValue)
        {
            var activeStats = await vwDashQuery.FirstOrDefaultAsync(v => v.ProjectId == selectedProjectId);
            
            ViewBag.TotalProjects   = 1;
            ViewBag.ActiveProjects  = (activeStats?.IsCompleted == false) ? 1 : 0;
            var selectedProject = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == selectedProjectId.Value && p.UserId == userId);
            var allocatedBudget = budgetBll.GetTotalBudget(selectedProjectId);
            ViewBag.TotalBudget     = selectedProject?.TotalBudget > 0 ? selectedProject.TotalBudget : allocatedBudget;
            ViewBag.TotalExpenses   = expenseBll.GetTotalSpent(selectedProjectId);
            ViewBag.ActiveProjectName = activeStats?.ProjectName;

            var totalWorkers = await GetProjectWorkersQuery(userId, selectedProjectId.Value).CountAsync();
            var workersOnSite = await GetProjectWorkersQuery(userId, selectedProjectId.Value)
                .CountAsync(w => w.IsActive);
            ViewBag.TotalWorkers = totalWorkers;
            ViewBag.WorkersOnSite = workersOnSite;
            ViewBag.WorkersInactive = Math.Max(0, totalWorkers - workersOnSite);

            ViewBag.TotalTasks      = activeStats?.TotalTasks ?? 0;
            ViewBag.TasksToDo       = await scopedTaskQuery.CountAsync(t => t.StatusId == 1);
            ViewBag.TasksInProgress = await scopedTaskQuery.CountAsync(t => t.StatusId == 2);
            ViewBag.TasksCompleted  = activeStats?.CompletedTasks ?? 0;
            ViewBag.TasksOverdue    = await scopedTaskQuery.CountAsync(t => t.StatusId == 1 && t.CreatedAt < DateTime.Now.AddDays(-7));
            
            ViewBag.Phases = await _context.Phases
                .AsNoTracking()
                .Include(p => p.PhaseType).Include(p => p.Tasks)
                .Where(p => p.ProjectId == selectedProjectId)
                .OrderBy(p => p.Sequence).ToListAsync();
        }
        else
        {
            var allStats = await vwDashQuery.ToListAsync();
            ViewBag.TotalProjects   = allStats.Count;
            ViewBag.ActiveProjects  = allStats.Count(s => !s.IsCompleted);
            var totalWorkers = await _context.Workers.CountAsync(w => w.UserId == userId);
            var workersOnSite = await _context.Workers.CountAsync(w => w.UserId == userId && w.IsActive);
            ViewBag.TotalWorkers = totalWorkers;
            ViewBag.WorkersOnSite = workersOnSite;
            ViewBag.WorkersInactive = Math.Max(0, totalWorkers - workersOnSite);
            
            var approvedBudget = await _context.Projects
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.TotalBudget);
            var allocatedBudget = budgetBll.GetTotalBudgetForUser(userId);
            ViewBag.TotalBudget     = approvedBudget > 0 ? approvedBudget : allocatedBudget;
            ViewBag.TotalExpenses   = expenseBll.GetTotalSpentForUser(userId);

            ViewBag.TotalTasks      = await taskQuery.CountAsync();
            ViewBag.TasksToDo       = await taskQuery.CountAsync(t => t.StatusId == 1);
            ViewBag.TasksInProgress = await taskQuery.CountAsync(t => t.StatusId == 2);
            ViewBag.TasksCompleted  = await taskQuery.CountAsync(t => t.StatusId == 3);
            ViewBag.TasksOverdue    = await taskQuery.CountAsync(t => t.StatusId == 1 && t.CreatedAt < DateTime.Now.AddDays(-7));
            
            ViewBag.ProjectsNotStarted = allStats.Count(s => (s.PhaseProgressPct ?? 0) == 0);
            ViewBag.ProjectsInProgress = allStats.Count(s => (s.PhaseProgressPct ?? 0) > 0 && !s.IsCompleted);
            ViewBag.ProjectsCompleted  = allStats.Count(s => s.IsCompleted);
            
            ViewBag.Projects = allStats;
        }

        // 5. Common Data
        ViewBag.RecentExpensesList = transactionBll
            .GetFilteredTransactions("", "", null, null, selectedProjectId, userId)
            .Where(t => !string.Equals(t.Category, "Project Budget", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        ViewBag.CategoryExpenses = expenseBll.GetExpensesByCategory(selectedProjectId, userId);

        ViewBag.MonthlyExpenses = await BuildMonthlySpendingTrendAsync(expenseBll, selectedProjectId, userId);
        ViewBag.AdvisorTriggers = await BuildDashboardAdvisorTriggersAsync(
            advisorBll,
            selectedProjectId,
            userId,
            ViewBag.TotalBudget is decimal totalBudgetValue ? totalBudgetValue : 0m,
            ViewBag.TotalExpenses is decimal totalExpenseValue ? totalExpenseValue : 0m);

        ViewBag.ToDoTasks = await scopedTaskQuery
            .AsNoTracking()
            .Include(t => t.Phase)
                .ThenInclude(p => p.PhaseType)
            .Where(t => t.StatusId == 1)
            .OrderBy(t => t.CreatedAt)
            .Take(6)
            .ToListAsync();
            
        ViewBag.WorkersOffSite  = 0;
        ViewBag.WorkersOnLeave  = 0;

        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> DashboardData(bool overall = false)
    {
        await _workerProjectSchema.EnsureAsync(HttpContext.RequestAborted);

        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null) return Unauthorized();
        int userId = int.Parse(userIdClaim.Value);

        int? selectedProjectId = overall ? null : HttpContext.Session.GetInt32("SelectedProjectId");
        if (selectedProjectId.HasValue)
        {
            bool ownsProject = await _context.Projects.AnyAsync(p => p.ProjectId == selectedProjectId.Value && p.UserId == userId);
            if (!ownsProject) selectedProjectId = null;
        }

        var connectionString = _configuration.GetConnectionString("BuildWise") ?? "";
        var budgetBll = new BudgetBLL(connectionString);
        var expenseBll = new ExpenseBLL(connectionString);
        var transactionBll = new TransactionBLL(connectionString);

        decimal allocatedBudget = selectedProjectId.HasValue
            ? budgetBll.GetTotalBudget(selectedProjectId)
            : budgetBll.GetTotalBudgetForUser(userId);
        decimal approvedBudget = selectedProjectId.HasValue
            ? await _context.Projects
                .Where(p => p.ProjectId == selectedProjectId.Value && p.UserId == userId)
                .Select(p => p.TotalBudget)
                .FirstOrDefaultAsync()
            : await _context.Projects
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.TotalBudget);
        decimal totalBudget = approvedBudget > 0 ? approvedBudget : allocatedBudget;
        decimal totalExpenses = selectedProjectId.HasValue
            ? expenseBll.GetTotalSpent(selectedProjectId)
            : expenseBll.GetTotalSpentForUser(userId);

        int totalWorkers = selectedProjectId.HasValue
            ? await GetProjectWorkersQuery(userId, selectedProjectId.Value).CountAsync()
            : await _context.Workers.CountAsync(w => w.UserId == userId);
        int workersOnSite = selectedProjectId.HasValue
            ? await GetProjectWorkersQuery(userId, selectedProjectId.Value).CountAsync(w => w.IsActive)
            : await _context.Workers.CountAsync(w => w.UserId == userId && w.IsActive);
        int workersInactive = Math.Max(0, totalWorkers - workersOnSite);

        var categoryExpenses = expenseBll.GetExpensesByCategory(selectedProjectId, userId);

        var recentExpenses = transactionBll
            .GetFilteredTransactions("", "", null, null, selectedProjectId, userId)
            .Where(t => !string.Equals(t.Category, "Project Budget", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .Select(t => new { t.TransactionDate, t.Category, t.Amount })
            .ToList();

        var monthlyExpenses = await BuildMonthlySpendingTrendAsync(expenseBll, selectedProjectId, userId);

        return Json(new
        {
            totalBudget,
            totalExpenses,
            totalWorkers,
            workersOnSite,
            workersInactive,
            categoryExpenses,
            recentExpenses,
            monthlyExpenses
        });
    }

    private async Task<List<MonthlyExpensePoint>> BuildMonthlySpendingTrendAsync(ExpenseBLL expenseBll, int? projectId, int userId)
    {
        var monthly = expenseBll.GetAllExpenses(projectId, userId)
            .GroupBy(e => new DateTime(e.ExpenseDate.Year, e.ExpenseDate.Month, 1))
            .Select(g => new MonthlyExpensePoint(g.Key.Year, g.Key.Month, g.Sum(e => e.Amount)))
            .ToList();

        var materialPurchases = await _context.MaterialPurchases
            .AsNoTracking()
            .Where(m => m.Project.UserId == userId && (!projectId.HasValue || m.ProjectId == projectId.Value))
            .GroupBy(m => new { m.PurchaseDate.Year, m.PurchaseDate.Month })
            .Select(g => new MonthlyExpensePoint(g.Key.Year, g.Key.Month, g.Sum(m => m.TotalCost ?? 0)))
            .ToListAsync();
        monthly.AddRange(materialPurchases);

        var wagePayments = await _context.WagePayments
            .AsNoTracking()
            .Where(w => w.Project.UserId == userId && (!projectId.HasValue || w.ProjectId == projectId.Value))
            .GroupBy(w => new { w.PaymentDate.Year, w.PaymentDate.Month })
            .Select(g => new MonthlyExpensePoint(g.Key.Year, g.Key.Month, g.Sum(w => w.AmountPaid)))
            .ToListAsync();
        monthly.AddRange(wagePayments);

        return monthly
            .GroupBy(e => new { e.Year, e.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyExpensePoint(g.Key.Year, g.Key.Month, g.Sum(e => e.Total)))
            .TakeLast(12)
            .ToList();
    }

    private IQueryable<Worker> GetProjectWorkersQuery(int userId, int projectId)
    {
        return _context.Workers.AsNoTracking().Where(w =>
            w.UserId == userId &&
            (w.ProjectId == projectId || w.WorkerProjectAssignments.Any(a => a.ProjectId == projectId)));
    }

    private async Task<List<DashboardAdvisorTrigger>> BuildDashboardAdvisorTriggersAsync(
        AdvisorBLL advisorBll,
        int? projectId,
        int userId,
        decimal totalBudget,
        decimal totalSpent)
    {
        var triggers = advisorBll.GetAnalysis(projectId, userId)
            .Where(r => !string.Equals(r.Severity, "Info", StringComparison.OrdinalIgnoreCase))
            .Take(4)
            .Select(r => new DashboardAdvisorTrigger(r.RuleName, r.Severity, r.Message, r.Category, "/Advisor"))
            .ToList();

        if (projectId.HasValue && totalBudget > 0)
        {
            var progress = await GetProjectProgressPercentAsync(projectId.Value);
            var budgetUsed = totalSpent / totalBudget * 100m;
            if (budgetUsed >= progress + 20m && totalSpent > 0)
            {
                triggers.Insert(0, new DashboardAdvisorTrigger(
                    "Cost Ahead of Progress",
                    budgetUsed >= progress + 35m ? "Warning" : "Caution",
                    $"Budget usage is {budgetUsed:0}% while construction progress is {progress:0}%. Review spending before releasing more purchases or expenses.",
                    "Progress",
                    "/Advisor"));
            }
        }

        if (projectId.HasValue)
        {
            var materialBalances = await _context.MaterialPurchases
                .AsNoTracking()
                .Where(p => p.ProjectId == projectId.Value)
                .Select(g => new
                {
                    g.MaterialId,
                    g.Material.MaterialName,
                    Purchased = g.Quantity,
                    Used = g.MaterialUsages.Sum(u => (decimal?)u.QuantityUsed) ?? 0m
                })
                .ToListAsync();

            var depletedMaterials = materialBalances
                .GroupBy(m => new { m.MaterialId, m.MaterialName })
                .Select(g => new
                {
                    g.Key.MaterialName,
                    Purchased = g.Sum(m => m.Purchased),
                    Used = g.Sum(m => m.Used)
                })
                .Where(x => x.Purchased > 0 && x.Used >= x.Purchased)
                .OrderBy(x => x.MaterialName)
                .Take(2)
                .ToList();

            foreach (var material in depletedMaterials)
            {
                triggers.Add(new DashboardAdvisorTrigger(
                    "Material Stock Depleted",
                    "Alert",
                    $"{material.MaterialName} stock is fully used. Check site demand before the next phase continues.",
                    "Materials",
                    "/Materials"));
            }
        }

        return triggers
            .GroupBy(t => new { t.Title, t.Category })
            .Select(g => g.First())
            .Take(5)
            .ToList();
    }

    private async Task<decimal> GetProjectProgressPercentAsync(int projectId)
    {
        var phases = await _context.Phases
            .AsNoTracking()
            .Include(p => p.Tasks)
            .Where(p => p.ProjectId == projectId)
            .ToListAsync();

        if (!phases.Any())
            return 0m;

        return Math.Round(phases.Average(p =>
        {
            if (p.Tasks.Count == 0)
                return p.IsCompleted ? 100m : 0m;

            return p.Tasks.Count(t => t.StatusId == 3) * 100m / p.Tasks.Count;
        }), 0);
    }

    private sealed record MonthlyExpensePoint(int Year, int Month, decimal Total);
}
