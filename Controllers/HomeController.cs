using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BuildWise.Models;
using BuildWise.BusinessLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BuildWise.Controllers;

public class HomeController : Controller
{
    private readonly BuildWiseDbContext _context;
    private readonly IConfiguration _configuration;

    public HomeController(BuildWiseDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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
        return View();
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
        return View();
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
    public async Task<IActionResult> Dashboard()
    {
        // 1. Get current User ID from claims
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null) return RedirectToAction("Index", "Account");
        int userId = int.Parse(userIdClaim.Value);

        // 2. Fetch Selected Project and Verify Ownership
        int? selectedProjectId = HttpContext.Session.GetInt32("SelectedProjectId");
        if (selectedProjectId.HasValue)
        {
            bool ownsProject = await _context.Projects.AnyAsync(p => p.ProjectId == selectedProjectId && p.UserId == userId);
            if (!ownsProject)
            {
                HttpContext.Session.Remove("SelectedProjectId");
                selectedProjectId = null;
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
        var vwExpQuery   = from v in _context.VwExpenseHistories
                          join p in _context.Projects on v.ProjectId equals p.ProjectId
                          where p.UserId == userId
                          select v;

        var budgetBll = new BudgetBLL(_configuration.GetConnectionString("BuildWise") ?? "");
        var expenseBll = new ExpenseBLL(_configuration.GetConnectionString("BuildWise") ?? "");

        // 4. Populate Stats
        if (selectedProjectId.HasValue)
        {
            var activeStats = await vwDashQuery.FirstOrDefaultAsync(v => v.ProjectId == selectedProjectId);
            
            ViewBag.TotalProjects   = 1;
            ViewBag.ActiveProjects  = (activeStats?.IsCompleted == false) ? 1 : 0;
            ViewBag.TotalBudget     = budgetBll.GetTotalBudget(selectedProjectId);
            ViewBag.TotalExpenses   = expenseBll.GetTotalSpent(selectedProjectId);
            ViewBag.ActiveProjectName = activeStats?.ProjectName;

            ViewBag.TotalWorkers = await _context.Attendances
                .Where(a => a.ProjectId == selectedProjectId && a.Project.UserId == userId)
                .Select(a => a.WorkerId).Distinct().CountAsync();

            ViewBag.WorkersOnSite = await _context.Attendances
                .Where(a => a.ProjectId == selectedProjectId && a.Project.UserId == userId && a.AttendanceDate == DateOnly.FromDateTime(DateTime.Today) && a.StatusId == 1)
                .CountAsync();

            ViewBag.TotalTasks      = activeStats?.TotalTasks ?? 0;
            ViewBag.TasksToDo       = await taskQuery.CountAsync(t => t.Phase.ProjectId == selectedProjectId && t.StatusId == 1);
            ViewBag.TasksInProgress = await taskQuery.CountAsync(t => t.Phase.ProjectId == selectedProjectId && t.StatusId == 2);
            ViewBag.TasksCompleted  = activeStats?.CompletedTasks ?? 0;
            ViewBag.TasksOverdue    = await taskQuery.CountAsync(t => t.Phase.ProjectId == selectedProjectId && t.StatusId == 1 && t.CreatedAt < DateTime.Now.AddDays(-7));
            
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
            ViewBag.TotalWorkers    = await _context.Workers.CountAsync(w => w.UserId == userId);
            ViewBag.WorkersOnSite   = await _context.Attendances
                .Where(a => a.Project.UserId == userId && a.AttendanceDate == DateOnly.FromDateTime(DateTime.Today) && a.StatusId == 1)
                .CountAsync();
            
            ViewBag.TotalBudget     = budgetBll.GetTotalBudgetForUser(userId);
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
        ViewBag.RecentExpensesList = await vwExpQuery
            .OrderByDescending(e => e.ExpenseDate).Take(5).ToListAsync();

        ViewBag.CategoryExpenses = await vwExpQuery
            .GroupBy(e => e.CategoryName)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync();
            
        ViewBag.WorkersOffSite  = 0;
        ViewBag.WorkersOnLeave  = 0;

        return View();
    }
}
