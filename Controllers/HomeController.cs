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
        var projectQuery = _context.Projects.Where(p => p.UserId == userId);
        var taskQuery    = _context.Tasks.Where(t => t.Phase.Project.UserId == userId);
        var vwDashQuery  = from v in _context.VwProjectDashboards
                          join p in _context.Projects on v.ProjectId equals p.ProjectId
                          where p.UserId == userId
                          select v;
        var scopedTaskQuery = selectedProjectId.HasValue
            ? taskQuery.Where(t => t.Phase.ProjectId == selectedProjectId.Value)
            : taskQuery;

        var connectionString = _configuration.GetConnectionString("BuildWise") ?? "";
        var budgetBll = new BudgetBLL(connectionString);
        var expenseBll = new ExpenseBLL(connectionString);
        var transactionBll = new TransactionBLL(connectionString);

        // 4. Populate Stats
        if (selectedProjectId.HasValue)
        {
            var activeStats = await vwDashQuery.FirstOrDefaultAsync(v => v.ProjectId == selectedProjectId);
            
            ViewBag.TotalProjects   = 1;
            ViewBag.ActiveProjects  = (activeStats?.IsCompleted == false) ? 1 : 0;
            var selectedProject = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == selectedProjectId.Value && p.UserId == userId);
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

        ViewBag.MonthlyExpenses = BuildMonthlyExpenseTrend(expenseBll, selectedProjectId, userId);

        ViewBag.ToDoTasks = await scopedTaskQuery
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

        var monthlyExpenses = BuildMonthlyExpenseTrend(expenseBll, selectedProjectId, userId);

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

    private static List<MonthlyExpensePoint> BuildMonthlyExpenseTrend(ExpenseBLL expenseBll, int? projectId, int userId)
    {
        return expenseBll.GetAllExpenses(projectId, userId)
            .GroupBy(e => new DateTime(e.ExpenseDate.Year, e.ExpenseDate.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new MonthlyExpensePoint(g.Key.Year, g.Key.Month, g.Sum(e => e.Amount)))
            .TakeLast(12)
            .ToList();
    }

    private IQueryable<Worker> GetProjectWorkersQuery(int userId, int projectId)
    {
        return _context.Workers.Where(w =>
            w.UserId == userId &&
            (w.ProjectId == projectId || w.WorkerProjectAssignments.Any(a => a.ProjectId == projectId)));
    }

    private sealed record MonthlyExpensePoint(int Year, int Month, decimal Total);
}
