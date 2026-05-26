using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BuildWise.Models;
using BuildWise.BusinessLayer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using BuildWise.Services;

namespace BuildWise.Controllers;

public class HomeController : BaseController
{
    private readonly BuildWiseDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly WorkerProjectSchemaService _workerProjectSchema;
    private readonly PropertyPhaseSchemaService _propertyPhaseSchema;

    public HomeController(BuildWiseDbContext context, IConfiguration configuration, WorkerProjectSchemaService workerProjectSchema, PropertyPhaseSchemaService propertyPhaseSchema)
    {
        _context = context;
        _configuration = configuration;
        _workerProjectSchema = workerProjectSchema;
        _propertyPhaseSchema = propertyPhaseSchema;
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
        await _propertyPhaseSchema.EnsureAsync(HttpContext.RequestAborted);

        // Get the current user.
        int userId = GetCurrentUserId();
        if (userId == 0) return RedirectToAction("Index", "Account");

        // Load the selected project.
        int? selectedProjectId = overall ? null : HttpContext.Session.GetInt32("SelectedProjectId");
        if (overall)
        {
            HttpContext.Session.Remove("SelectedProjectId");
        }
        ViewBag.IsOverallDashboard = overall;
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

        // Build user-scoped queries.
        var projectQuery = _context.Projects.AsNoTracking().Where(p => p.UserId == userId);
        var taskQuery    = _context.Tasks.AsNoTracking().Where(t => t.Phase.Project.UserId == userId);
        var vwDashQuery  = from v in _context.VwProjectDashboards.AsNoTracking()
                          join p in _context.Projects.AsNoTracking() on v.ProjectId equals p.ProjectId
                          where p.UserId == userId
                          select v;
        var scopedTaskQuery = selectedProjectId.HasValue
            ? taskQuery.Where(t => t.Phase.ProjectId == selectedProjectId.Value)
            : taskQuery;
        var scopedPropertyQuery = selectedProjectId.HasValue
            ? _context.Properties.AsNoTracking().Where(p => p.UserId == userId && p.ProjectId == selectedProjectId.Value)
            : _context.Properties.AsNoTracking().Where(p => p.UserId == userId && p.ProjectId != null);

        var connectionString = _configuration.GetConnectionString("BuildWise") ?? "";
        var budgetBll = new BudgetBLL(connectionString);
        var expenseBll = new ExpenseBLL(connectionString);
        var transactionBll = new TransactionBLL(connectionString);
        var advisorBll = new AdvisorBLL(connectionString);

        // Fill dashboard stats.
        if (selectedProjectId.HasValue)
        {
            var activeStats = await vwDashQuery.FirstOrDefaultAsync(v => v.ProjectId == selectedProjectId);
            
            ViewBag.TotalProjects   = 1;
            ViewBag.TotalProperties = await scopedPropertyQuery.CountAsync();
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
            ViewBag.ProjectProgress = await GetProjectProgressPercentAsync(selectedProjectId.Value);
            
            ViewBag.Phases = await _context.Phases
                .AsNoTracking()
                .Include(p => p.PhaseType).Include(p => p.Property).Include(p => p.Tasks)
                .Where(p => p.ProjectId == selectedProjectId)
                .OrderBy(p => p.Sequence).ToListAsync();
        }
        else
        {
            var allStats = await vwDashQuery.ToListAsync();
            var overallProjects = allStats
                .Select(s =>
                {
                    var progress = (decimal)(s.TaskProgressPct ?? s.PhaseProgressPct ?? (s.IsCompleted ? 100d : 0d));
                    var totalBudget = s.TotalBudget;
                    var totalSpent = s.TotalSpent ?? s.TotalExpenses;
                    return new ProjectDashboardSummary(
                        s.ProjectId,
                        s.ProjectName,
                        s.PropertyName,
                        totalBudget,
                        totalSpent,
                        Math.Max(0, totalBudget - totalSpent),
                        Math.Round(progress, 0),
                        s.TotalTasks ?? 0,
                        s.CompletedTasks ?? 0,
                        s.IsCompleted);
                })
                .OrderBy(p => p.IsCompleted)
                .ThenByDescending(p => p.ProgressPercent)
                .ThenBy(p => p.ProjectName)
                .ToList();

            ViewBag.TotalProjects   = allStats.Count;
            ViewBag.TotalProperties = await scopedPropertyQuery.CountAsync();
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

            ViewBag.OverallConstructionProgress = overallProjects.Count > 0
                ? Math.Round(overallProjects.Average(p => p.ProgressPercent), 0)
                : 0m;
            ViewBag.OverallProjects = overallProjects;
            ViewBag.Projects = allStats;
        }

        // Fill shared dashboard data.
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
            .Include(t => t.Phase)
                .ThenInclude(p => p.Property)
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
        await _propertyPhaseSchema.EnsureAsync(HttpContext.RequestAborted);

        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        int? selectedProjectId = overall ? null : GetSelectedProjectId();
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
        int totalProperties = selectedProjectId.HasValue
            ? await _context.Properties.CountAsync(p => p.UserId == userId && p.ProjectId == selectedProjectId.Value)
            : await _context.Properties.CountAsync(p => p.UserId == userId && p.ProjectId != null);
        int totalProjects = selectedProjectId.HasValue
            ? 1
            : await _context.Projects.CountAsync(p => p.UserId == userId);

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
        var projectStatus = selectedProjectId.HasValue
            ? null
            : await BuildOverallProjectSummariesAsync(userId);

        return Json(new
        {
            totalBudget,
            totalExpenses,
            totalProjects,
            totalProperties,
            totalWorkers,
            workersOnSite,
            workersInactive,
            categoryExpenses,
            recentExpenses,
            monthlyExpenses,
            projectStatus
        });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> DownloadDashboardReport(DashboardReportOptions options)
    {
        await _workerProjectSchema.EnsureAsync(HttpContext.RequestAborted);

        int userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        int? selectedProjectId = options.Overall ? null : GetSelectedProjectId();
        if (selectedProjectId.HasValue)
        {
            bool ownsProject = await _context.Projects.AnyAsync(p => p.ProjectId == selectedProjectId.Value && p.UserId == userId);
            if (!ownsProject) selectedProjectId = null;
        }

        if (!selectedProjectId.HasValue && !options.Overall)
        {
            selectedProjectId = await _context.Projects
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.ProjectName == "main" ? 0 : 1)
                .ThenBy(p => p.ProjectName)
                .Select(p => (int?)p.ProjectId)
                .FirstOrDefaultAsync();
        }

        var reportData = await BuildDashboardReportDataAsync(userId, selectedProjectId);
        var pdf = new DashboardReportPdfBuilder(reportData, options).Build();
        var fileScope = selectedProjectId.HasValue ? reportData.ScopeName : "Overall";
        var fileName = $"BuildWise-{SanitizeFileName(fileScope)}-Report-{DateTime.Now:yyyyMMdd}.pdf";

        return File(pdf, "application/pdf", fileName);
    }

    private async Task<List<MonthlyExpensePoint>> BuildMonthlySpendingTrendAsync(ExpenseBLL expenseBll, int? projectId, int userId)
    {
        var monthly = expenseBll.GetAllExpenses(projectId, userId)
            .GroupBy(e => new DateTime(e.ExpenseDate.Year, e.ExpenseDate.Month, 1))
            .Select(g => new MonthlyExpensePoint(g.Key.Year, g.Key.Month, g.Sum(e => e.Amount)))
            .ToList();

        var mirroredMaterialExpenseDescriptions = expenseBll.GetAllExpenses(projectId, userId)
            .Where(e => string.Equals(e.Category, "Material", StringComparison.OrdinalIgnoreCase)
                && e.Description.StartsWith("Material purchase #", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Description)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var materialPurchaseRows = await _context.MaterialPurchases
            .AsNoTracking()
            .Where(m => m.Project.UserId == userId && (!projectId.HasValue || m.ProjectId == projectId.Value))
            .Select(m => new
            {
                m.PurchaseId,
                m.PurchaseDate,
                Total = m.TotalCost ?? 0
            })
            .ToListAsync();
        var materialPurchases = materialPurchaseRows
            .Where(m => !mirroredMaterialExpenseDescriptions.Any(d => d.StartsWith($"Material purchase #{m.PurchaseId}", StringComparison.OrdinalIgnoreCase)))
            .GroupBy(m => new { m.PurchaseDate.Year, m.PurchaseDate.Month })
            .Select(g => new MonthlyExpensePoint(g.Key.Year, g.Key.Month, g.Sum(m => m.Total)))
            .ToList();
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

    private async Task<List<ProjectDashboardSummary>> BuildOverallProjectSummariesAsync(int userId)
    {
        var stats = await (from v in _context.VwProjectDashboards.AsNoTracking()
                           join p in _context.Projects.AsNoTracking() on v.ProjectId equals p.ProjectId
                           where p.UserId == userId
                           select v)
            .ToListAsync();

        var progressByProject = new Dictionary<int, decimal>();
        foreach (var projectId in stats.Select(s => s.ProjectId).Distinct())
        {
            progressByProject[projectId] = await GetProjectProgressPercentAsync(projectId);
        }

        return stats
            .Select(s =>
            {
                var progress = progressByProject.TryGetValue(s.ProjectId, out var calculatedProgress)
                    ? calculatedProgress
                    : (decimal)(s.TaskProgressPct ?? s.PhaseProgressPct ?? (s.IsCompleted ? 100d : 0d));
                var totalSpent = s.TotalSpent ?? s.TotalExpenses;
                return new ProjectDashboardSummary(
                    s.ProjectId,
                    s.ProjectName,
                    s.PropertyName,
                    s.TotalBudget,
                    totalSpent,
                    Math.Max(0, s.TotalBudget - totalSpent),
                    Math.Round(progress, 0),
                    s.TotalTasks ?? 0,
                    s.CompletedTasks ?? 0,
                    s.IsCompleted);
            })
            .OrderBy(p => p.IsCompleted)
            .ThenByDescending(p => p.ProgressPercent)
            .ThenBy(p => p.ProjectName)
            .ToList();
    }

    private async Task<DashboardReportData> BuildDashboardReportDataAsync(int userId, int? projectId)
    {
        var connectionString = _configuration.GetConnectionString("BuildWise") ?? "";
        var budgetBll = new BudgetBLL(connectionString);
        var expenseBll = new ExpenseBLL(connectionString);
        var transactionBll = new TransactionBLL(connectionString);

        var projectQuery = _context.Projects.AsNoTracking().Where(p => p.UserId == userId);
        if (projectId.HasValue)
            projectQuery = projectQuery.Where(p => p.ProjectId == projectId.Value);

        var isOverallReport = !projectId.HasValue;
        var scopeName = projectId.HasValue
            ? await _context.Projects.AsNoTracking()
                .Where(p => p.ProjectId == projectId.Value && p.UserId == userId)
                .Select(p => p.ProjectName)
                .FirstOrDefaultAsync() ?? "Project"
            : "Overall Stats";

        decimal allocatedBudget = projectId.HasValue
            ? budgetBll.GetTotalBudget(projectId)
            : budgetBll.GetTotalBudgetForUser(userId);
        decimal approvedBudget = projectId.HasValue
            ? await _context.Projects
                .Where(p => p.ProjectId == projectId.Value && p.UserId == userId)
                .Select(p => p.TotalBudget)
                .FirstOrDefaultAsync()
            : await _context.Projects
                .Where(p => p.UserId == userId)
                .SumAsync(p => p.TotalBudget);
        decimal totalBudget = approvedBudget > 0 ? approvedBudget : allocatedBudget;
        decimal totalExpenses = projectId.HasValue
            ? expenseBll.GetTotalSpent(projectId)
            : expenseBll.GetTotalSpentForUser(userId);

        var dashboardRows = await (from v in _context.VwProjectDashboards.AsNoTracking()
                                   join p in _context.Projects.AsNoTracking() on v.ProjectId equals p.ProjectId
                                   where p.UserId == userId && (!projectId.HasValue || v.ProjectId == projectId.Value)
                                   orderby v.IsCompleted, v.ProjectName
                                   select v)
            .ToListAsync();
        var projectReportRows = dashboardRows
            .Select(p =>
            {
                var progress = (decimal)(p.TaskProgressPct ?? p.PhaseProgressPct ?? (p.IsCompleted ? 100d : 0d));
                var spent = p.TotalSpent ?? p.TotalExpenses;
                return new ProjectReportRow(
                    p.ProjectName,
                    p.PropertyName,
                    p.TotalBudget,
                    spent,
                    Math.Max(0, p.TotalBudget - spent),
                    Math.Round(progress, 0),
                    p.IsCompleted ? "Completed" : "Active");
            })
            .ToList();

        var workersQuery = projectId.HasValue
            ? GetProjectWorkersQuery(userId, projectId.Value)
            : _context.Workers.AsNoTracking().Where(w => w.UserId == userId);
        var totalWorkers = await workersQuery.CountAsync();
        var activeWorkers = await workersQuery.CountAsync(w => w.IsActive);
        var averageDailyWage = totalWorkers > 0 ? await workersQuery.AverageAsync(w => w.DailyWage) : 0m;
        var workerSkillValues = await workersQuery
            .Select(w => w.SkillType)
            .ToListAsync();
        var workerSkills = workerSkillValues
            .GroupBy(skill => string.IsNullOrWhiteSpace(skill) ? "Unspecified" : skill)
            .Select(g => new NameCount(g.Key!, g.Count()))
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.Name)
            .ToList();

        var propertyRows = await projectQuery
            .Include(p => p.Property).ThenInclude(p => p.Type)
            .Include(p => p.Property).ThenInclude(p => p.Status)
            .Include(p => p.Property).ThenInclude(p => p.AreaUnit)
            .ToListAsync();
        var properties = propertyRows
            .Select(p => new PropertyReportRow(
                p.Property.PropertyName,
                p.Property.Type.TypeName,
                string.IsNullOrWhiteSpace(p.Property.City) ? p.Property.Location : p.Property.Location + ", " + p.Property.City,
                p.Property.AreaSize.ToString("0.##") + " " + p.Property.AreaUnit.UnitName,
                p.Property.Status.StatusName))
            .Distinct()
            .ToList();

        var expenseCategories = expenseBll.GetExpensesByCategory(projectId, userId)
            .OrderByDescending(e => e.Amount)
            .Select(e => new NameAmount(e.Category, e.Amount))
            .ToList();

        var recentExpenses = transactionBll
            .GetFilteredTransactions("", "", null, null, projectId, userId)
            .Where(t => !string.Equals(t.Category, "Project Budget", StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .Select(t => new RecentExpenseReportRow(t.TransactionDate.ToString("MMM dd, yyyy"), t.Category, t.Amount))
            .ToList();

        var materialRows = await _context.MaterialPurchases
            .AsNoTracking()
            .Where(m => m.Project.UserId == userId && (!projectId.HasValue || m.ProjectId == projectId.Value))
            .Select(m => new
            {
                m.Material.MaterialName,
                m.Unit.UnitName,
                Purchased = m.Quantity,
                Used = m.MaterialUsages.Sum(u => (decimal?)u.QuantityUsed) ?? 0m,
                Cost = m.TotalCost ?? 0m
            })
            .ToListAsync();
        var materials = materialRows
            .GroupBy(m => new { m.MaterialName, m.UnitName })
            .Select(g =>
            {
                var purchased = g.Sum(x => x.Purchased);
                var used = g.Sum(x => x.Used);
                return new MaterialReportRow(g.Key.MaterialName, g.Key.UnitName, purchased, used, purchased - used, g.Sum(x => x.Cost));
            })
            .OrderByDescending(m => m.Cost)
            .ToList();

        var phases = await _context.Phases
            .AsNoTracking()
            .Include(p => p.Tasks)
            .Where(p => p.Project.UserId == userId && (!projectId.HasValue || p.ProjectId == projectId.Value))
            .ToListAsync();
        var reportTasks = phases.SelectMany(p => p.Tasks).ToList();
        decimal constructionProgress = reportTasks.Count > 0
            ? Math.Round(reportTasks.Count(t => t.StatusId == 3) * 100m / reportTasks.Count, 2)
            : phases.Count > 0
                ? Math.Round(phases.Count(p => p.IsCompleted) * 100m / phases.Count, 2)
                : 0m;

        var monthlyTrend = (await BuildMonthlySpendingTrendAsync(expenseBll, projectId, userId))
            .Select(m => new NameAmount($"{CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames[m.Month - 1]} {m.Year}", m.Total))
            .ToList();

        return new DashboardReportData
        {
            IsOverall = isOverallReport,
            ScopeName = scopeName,
            TotalProjects = projectReportRows.Count,
            ActiveProjects = projectReportRows.Count(p => string.Equals(p.Status, "Active", StringComparison.OrdinalIgnoreCase)),
            CompletedProjects = projectReportRows.Count(p => string.Equals(p.Status, "Completed", StringComparison.OrdinalIgnoreCase)),
            AverageProjectProgress = projectReportRows.Count > 0 ? Math.Round(projectReportRows.Average(p => p.ProgressPercent), 0) : 0m,
            TotalBudget = totalBudget,
            TotalExpenses = totalExpenses,
            TotalWorkers = totalWorkers,
            ActiveWorkers = activeWorkers,
            AverageDailyWage = averageDailyWage,
            ConstructionProgress = constructionProgress,
            TotalMaterialPurchased = materials.Sum(m => m.Purchased),
            TotalMaterialUsed = materials.Sum(m => m.Used),
            Projects = projectReportRows,
            Properties = properties,
            WorkerSkills = workerSkills,
            ExpenseCategories = expenseCategories,
            RecentExpenses = recentExpenses,
            Materials = materials,
            MonthlyTrend = monthlyTrend
        };
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "Dashboard" : clean.Replace(' ', '-');
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
        var propertyStatuses = await _context.Properties
            .AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .Select(p => p.Status.StatusName)
            .ToListAsync();
        if (propertyStatuses.Any() && propertyStatuses.All(s => string.Equals(s, "Completed", StringComparison.OrdinalIgnoreCase)))
            return 100m;

        var phases = await _context.Phases
            .AsNoTracking()
            .Include(p => p.Tasks)
            .Where(p => p.ProjectId == projectId)
            .ToListAsync();

        if (!phases.Any())
        {
            if (!propertyStatuses.Any())
                return 0m;

            var completedProperties = propertyStatuses.Count(s => string.Equals(s, "Completed", StringComparison.OrdinalIgnoreCase));
            return Math.Round(completedProperties * 100m / propertyStatuses.Count, 0);
        }

        return Math.Round(phases.Average(p =>
        {
            if (p.Tasks.Count == 0)
                return p.IsCompleted ? 100m : 0m;

            return p.Tasks.Count(t => t.StatusId == 3) * 100m / p.Tasks.Count;
        }), 0);
    }

    private sealed record MonthlyExpensePoint(int Year, int Month, decimal Total);
}
