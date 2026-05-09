using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BuildWise.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Controllers;

public class HomeController : Controller
{
    private readonly BuildWiseDbContext _context;

    public HomeController(BuildWiseDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
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

    public IActionResult FAQ()
    {
        return View();
    }

    public IActionResult Contact()
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

    public async Task<IActionResult> Dashboard()
    {
        // Get active project from session
        int? selectedProjectId = HttpContext.Session.GetInt32("SelectedProjectId");

        // Base queries
        var projectQuery = _context.Projects.AsQueryable();
        var taskQuery    = _context.Tasks.AsQueryable();
        var expenseQuery = _context.Expenses.AsQueryable();
        var budgetQuery  = _context.Budgets.AsQueryable();
        var vwDashQuery  = _context.VwProjectDashboards.AsQueryable();
        var vwExpQuery   = _context.VwExpenseHistories.AsQueryable();

        // Apply filters if a project is selected
        if (selectedProjectId.HasValue)
        {
            projectQuery = projectQuery.Where(p => p.ProjectId == selectedProjectId);
            taskQuery    = taskQuery.Where(t => t.Phase.ProjectId == selectedProjectId); // Linking through Phase
            expenseQuery = expenseQuery.Where(e => e.ProjectId == selectedProjectId);
            budgetQuery  = budgetQuery.Where(b => b.ProjectId == selectedProjectId);
            vwDashQuery  = vwDashQuery.Where(v => v.ProjectId == selectedProjectId);
            vwExpQuery   = vwExpQuery.Where(v => v.ProjectId == selectedProjectId);
            
            ViewBag.ActiveProjectName = await _context.Projects
                .Where(p => p.ProjectId == selectedProjectId)
                .Select(p => p.ProjectName)
                .FirstOrDefaultAsync();
        }

        // 1. Top Stats
        ViewBag.TotalProjects   = await projectQuery.CountAsync();
        ViewBag.ActiveProjects  = await projectQuery.CountAsync(p => !p.IsCompleted);
        
        // Workers count (tricky if project filtered, we look for workers assigned to tasks in this project)
        if (selectedProjectId.HasValue) {
            ViewBag.TotalWorkers = await _context.TaskWorkers
                .Where(tw => tw.Task.Phase.ProjectId == selectedProjectId)
                .Select(tw => tw.WorkerId)
                .Distinct()
                .CountAsync();
        } else {
            ViewBag.TotalWorkers = await _context.Workers.CountAsync();
        }

        ViewBag.TotalTasks      = await taskQuery.CountAsync();
        ViewBag.PendingTasks    = await taskQuery.CountAsync(t => t.StatusId == 1);

        ViewBag.TotalBudget     = await budgetQuery.SumAsync(b => b.TotalBudget);
        ViewBag.TotalExpenses   = await expenseQuery.SumAsync(e => e.Amount);

        // 2. Project Status Breakdown
        ViewBag.ProjectsNotStarted = await projectQuery.CountAsync(p => !p.Phases.Any());
        ViewBag.ProjectsInProgress = await projectQuery.CountAsync(p => !p.IsCompleted && p.Phases.Any());
        ViewBag.ProjectsCompleted  = await projectQuery.CountAsync(p => p.IsCompleted);
        ViewBag.ProjectsOnHold     = 0;

        // 3. Monthly Data
        var sixMonthsAgo = DateOnly.FromDateTime(DateTime.Today.AddMonths(-6));
        ViewBag.MonthlyExpenses = await expenseQuery
            .Where(e => e.ExpenseDate >= sixMonthsAgo)
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(e => e.Amount) })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();

        // 4. Expenses by Category
        ViewBag.CategoryExpenses = await vwExpQuery
            .GroupBy(e => e.CategoryName)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync();

        // 5. Recent Projects
        ViewBag.Projects = await vwDashQuery
            .OrderByDescending(p => p.ProjectId)
            .Take(5)
            .ToListAsync();

        // 6. Recent Expenses
        ViewBag.RecentExpensesList = await vwExpQuery
            .OrderByDescending(e => e.ExpenseDate)
            .Take(5)
            .ToListAsync();

        // 7. Tasks Summary
        ViewBag.TasksToDo       = await taskQuery.CountAsync(t => t.StatusId == 1);
        ViewBag.TasksInProgress = await taskQuery.CountAsync(t => t.StatusId == 2);
        ViewBag.TasksCompleted  = await taskQuery.CountAsync(t => t.StatusId == 3);
        ViewBag.TasksOverdue    = await taskQuery.CountAsync(t => t.StatusId == 1 && t.CreatedAt < DateTime.Now.AddDays(-7));

        // 8. Active Workers Summary
        if (selectedProjectId.HasValue) {
            ViewBag.WorkersOnSite = await _context.TaskWorkers
                .Where(tw => tw.Task.Phase.ProjectId == selectedProjectId)
                .Select(tw => tw.WorkerId)
                .Distinct()
                .CountAsync();
        } else {
            ViewBag.WorkersOnSite = await _context.Workers.CountAsync(w => w.IsActive == true);
        }
        
        ViewBag.WorkersOffSite  = 0;
        ViewBag.WorkersOnLeave  = 0;

        return View();
    }

    public async Task<IActionResult> CheckUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Json(users);
    }
}
