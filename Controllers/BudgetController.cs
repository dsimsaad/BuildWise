using Microsoft.AspNetCore.Mvc;
using BuildWise.Models;
using BuildWise.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;

namespace BuildWise.Controllers
{
    [Authorize]
    public class BudgetController : Controller
    {
        private readonly string _connectionString;
        private readonly BudgetBLL _budgetBll;
        private readonly ExpenseBLL _expenseBll;
        private readonly TransactionBLL _transactionBll;
        private readonly BuildWiseDbContext _context;

        public BudgetController(IConfiguration configuration, BuildWiseDbContext context)
        {
            _connectionString = configuration.GetConnectionString("BuildWise") ?? "";
            _budgetBll = new BudgetBLL(_connectionString);
            _expenseBll = new ExpenseBLL(_connectionString);
            _transactionBll = new TransactionBLL(_connectionString);
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        private int? GetSelectedProjectId()
        {
            return HttpContext.Session.GetInt32("SelectedProjectId");
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        private bool UserOwnsProject(int projectId, int userId)
        {
            return _context.Projects.Any(p => p.ProjectId == projectId && p.UserId == userId);
        }

        private bool BudgetBelongsToUser(int budgetId, int userId, out BudgetItem? budget)
        {
            budget = _budgetBll.GetBudgetById(budgetId);
            return budget?.ProjectId != null && UserOwnsProject(budget.ProjectId.Value, userId);
        }

        private bool ExpenseBelongsToUser(int expenseId, int userId, out ExpenseItem? expense)
        {
            expense = _expenseBll.GetExpenseById(expenseId);
            return expense?.ProjectId != null && UserOwnsProject(expense.ProjectId.Value, userId);
        }

        [HttpGet]
        public IActionResult GetData()
        {
            int userId = GetCurrentUserId();
            var selectedProjectId = GetSelectedProjectId();
            if (selectedProjectId.HasValue && !UserOwnsProject(selectedProjectId.Value, userId))
            {
                HttpContext.Session.Remove("SelectedProjectId");
                selectedProjectId = null;
            }

            var budgets = _budgetBll.GetAllBudgets(selectedProjectId, userId);
            var expenses = _expenseBll.GetAllExpenses(selectedProjectId, userId);
            var totalBudget = selectedProjectId.HasValue 
                ? _budgetBll.GetTotalBudget(selectedProjectId) 
                : _budgetBll.GetTotalBudgetForUser(userId);
            var totalSpent = selectedProjectId.HasValue 
                ? _expenseBll.GetTotalSpent(selectedProjectId) 
                : _expenseBll.GetTotalSpentForUser(userId);
            
            var categoryExpenses = _expenseBll.GetExpensesByCategory(selectedProjectId, userId);

            var project = selectedProjectId.HasValue
                ? _context.Projects.FirstOrDefault(p => p.ProjectId == selectedProjectId.Value && p.UserId == userId)
                : null;
            var projectTotalBudget = project?.TotalBudget ?? 0;

            return Json(new { 
                budgets, 
                expenses, 
                totalBudget, 
                totalSpent,
                categoryExpenses,
                projectTotalBudget,
                selectedProjectId
            });
        }

        [HttpPost]
        public IActionResult AddBudget([FromBody] BudgetItem item)
        {
            int userId = GetCurrentUserId();
            var projectId = GetSelectedProjectId();
            if (projectId == null)
                return Json(new { success = false, message = "Please select a project first." });
            if (!UserOwnsProject(projectId.Value, userId))
                return Forbid();

            var project = _context.Projects.FirstOrDefault(p => p.ProjectId == projectId.Value && p.UserId == userId);
            var currentAllocated = _budgetBll.GetTotalBudget(projectId.Value);
            
            if (project != null && project.TotalBudget > 0 && (currentAllocated + item.Amount) > project.TotalBudget)
            {
                var formattedBudget = project.TotalBudget.ToString("N0", new CultureInfo("en-IN"));
                return Json(new { success = false, message = $"Category budget exceeds the Total Project Budget (PKR {formattedBudget}). Please increase the Total Budget first." });
            }

            item.ProjectId = projectId;
            if (_budgetBll.AddBudget(item))
                return Json(new { success = true });
            return Json(new { success = false, message = "Invalid data" });
        }

        [HttpPost]
        public IActionResult UpdateTotalProjectBudget([FromBody] decimal amount)
        {
            int userId = GetCurrentUserId();
            var projectId = GetSelectedProjectId();
            if (projectId == null)
                return Json(new { success = false, message = "Please select a project first." });

            var project = _context.Projects.FirstOrDefault(p => p.ProjectId == projectId.Value && p.UserId == userId);
            if (project != null)
            {
                project.TotalBudget = amount;
                _context.SaveChanges();
                _transactionBll.AddTransaction(new TransactionLog
                {
                    ProjectId = projectId.Value,
                    TransactionType = "Updated",
                    Category = "Project Budget",
                    Description = "Total project budget updated.",
                    Amount = amount,
                    BudgetEffect = 0
                });
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Project not found" });
        }

        [HttpPost]
        public IActionResult UpdateBudget([FromBody] BudgetItem item)
        {
            int userId = GetCurrentUserId();
            if (!BudgetBelongsToUser(item.BudgetId, userId, out var existingBudget))
                return Forbid();

            item.ProjectId = existingBudget!.ProjectId;
            if (_budgetBll.UpdateBudget(item))
                return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult DeleteBudget(int id)
        {
            int userId = GetCurrentUserId();
            if (!BudgetBelongsToUser(id, userId, out _))
                return Forbid();

            if (_budgetBll.DeleteBudget(id))
                return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult AddExpense([FromBody] ExpenseItem item)
        {
            int userId = GetCurrentUserId();
            var projectId = GetSelectedProjectId();
            if (projectId == null)
                return Json(new { success = false, message = "Please select a project first." });
            if (!UserOwnsProject(projectId.Value, userId))
                return Forbid();

            item.ProjectId = projectId;
            item.Description = string.IsNullOrWhiteSpace(item.Description) ? "" : item.Description.Trim();
            if (_expenseBll.AddExpense(item))
                return Json(new { success = true });
            return Json(new { success = false, message = "Invalid data" });
        }

        [HttpPost]
        public IActionResult UpdateExpense([FromBody] ExpenseItem item)
        {
            int userId = GetCurrentUserId();
            if (!ExpenseBelongsToUser(item.ExpenseId, userId, out var existingExpense))
                return Forbid();

            item.ProjectId = existingExpense!.ProjectId;
            if (_expenseBll.UpdateExpense(item))
                return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult DeleteExpense(int id)
        {
            int userId = GetCurrentUserId();
            if (!ExpenseBelongsToUser(id, userId, out _))
                return Forbid();

            if (_expenseBll.DeleteExpense(id))
                return Json(new { success = true });
            return Json(new { success = false });
        }
    }
}
