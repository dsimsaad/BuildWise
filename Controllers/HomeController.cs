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
        // 1. Top Stats & Trends
        ViewBag.TotalProjects   = await _context.Projects.CountAsync();
        ViewBag.ActiveProjects  = await _context.Projects.CountAsync(p => !p.IsCompleted);
        ViewBag.TotalWorkers    = await _context.Workers.CountAsync();
        ViewBag.TotalTasks      = await _context.Tasks.CountAsync();
        ViewBag.PendingTasks    = await _context.Tasks.CountAsync(t => t.StatusId == 1); // Assuming 1 = Pending

        ViewBag.TotalBudget     = await _context.Budgets.SumAsync(b => b.TotalBudget);
        ViewBag.TotalExpenses   = await _context.Expenses.SumAsync(e => e.Amount);

        // 2. Project Status Breakdown (For Doughnut Chart)
        ViewBag.ProjectsNotStarted = await _context.Projects.CountAsync(p => !p.Phases.Any());
        ViewBag.ProjectsInProgress = await _context.Projects.CountAsync(p => !p.IsCompleted && p.Phases.Any());
        ViewBag.ProjectsCompleted  = await _context.Projects.CountAsync(p => p.IsCompleted);
        ViewBag.ProjectsOnHold     = 0; // Assuming we don't have a specific Hold status in basic DB

        // 3. Monthly Budget vs Spent (Last 6 Months)
        var sixMonthsAgo = DateOnly.FromDateTime(DateTime.Today.AddMonths(-6));
        var monthlyData = await _context.Expenses
            .Where(e => e.ExpenseDate >= sixMonthsAgo)
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Total = g.Sum(e => e.Amount) })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();
        ViewBag.MonthlyExpenses = monthlyData;

        // 4. Expenses by Category (For Doughnut Chart)
        var categoryExpenses = await _context.VwExpenseHistories
            .GroupBy(e => e.CategoryName)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync();
        ViewBag.CategoryExpenses = categoryExpenses;

        // 5. Recent Projects Table
        var projects = await _context.VwProjectDashboards
            .OrderByDescending(p => p.ProjectId)
            .Take(5)
            .ToListAsync();
        ViewBag.Projects = projects;

        // 6. Recent Expenses Table
        var recentExpenses = await _context.VwExpenseHistories
            .OrderByDescending(e => e.ExpenseDate)
            .Take(5)
            .ToListAsync();
        ViewBag.RecentExpensesList = recentExpenses;

        // 7. Tasks Summary
        ViewBag.TasksToDo       = await _context.Tasks.CountAsync(t => t.StatusId == 1); // To Do
        ViewBag.TasksInProgress = await _context.Tasks.CountAsync(t => t.StatusId == 2); // In Progress
        ViewBag.TasksCompleted  = await _context.Tasks.CountAsync(t => t.StatusId == 3); // Completed
        ViewBag.TasksOverdue    = await _context.Tasks.CountAsync(t => t.StatusId == 1 && t.CreatedAt < DateTime.Now.AddDays(-7)); // Mock logic for overdue

        // 8. Active Workers Summary
        ViewBag.WorkersOnSite   = await _context.Workers.CountAsync(w => w.IsActive == true);
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
