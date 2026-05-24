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

        private static string FormatPkr(decimal amount)
        {
            return $"PKR {amount.ToString("N0", new CultureInfo("en-IN"))}";
        }

        private decimal GetCategoryAllocatedAmount(int projectId, string category, int? excludeBudgetId = null)
        {
            return _budgetBll.GetAllBudgets(projectId)
                .Where(b => !excludeBudgetId.HasValue || b.BudgetId != excludeBudgetId.Value)
                .Where(b => string.Equals(b.Category, category, StringComparison.OrdinalIgnoreCase))
                .Sum(b => b.Amount);
        }

        private decimal GetCategorySpentAmount(int projectId, int userId, string category, int? excludeExpenseId = null)
        {
            var spent = _expenseBll.GetExpensesByCategory(projectId, userId)
                .FirstOrDefault(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase))
                ?.Amount ?? 0m;

            if (excludeExpenseId.HasValue)
            {
                var existingExpense = _expenseBll.GetExpenseById(excludeExpenseId.Value);
                if (existingExpense != null && string.Equals(existingExpense.Category, category, StringComparison.OrdinalIgnoreCase))
                {
                    spent -= existingExpense.Amount;
                }
            }

            return Math.Max(0, spent);
        }

        private string? ValidateBudgetAllocation(int projectId, int userId, string category, decimal newCategoryAllocation, decimal newProjectAllocationTotal)
        {
            if (string.IsNullOrWhiteSpace(category))
                return "Please select a valid category.";
            if (newCategoryAllocation <= 0)
                return "Budget allocation must be greater than zero.";

            var project = _context.Projects.FirstOrDefault(p => p.ProjectId == projectId && p.UserId == userId);
            if (project != null && project.TotalBudget > 0 && newProjectAllocationTotal > project.TotalBudget)
            {
                return $"Category allocations cannot exceed the Total Project Budget ({FormatPkr(project.TotalBudget)}). Please increase the Total Budget first.";
            }

            var spentInCategory = GetCategorySpentAmount(projectId, userId, category);
            if (spentInCategory > newCategoryAllocation)
            {
                return $"{category} already has {FormatPkr(spentInCategory)} in expenses. You cannot allocate only {FormatPkr(newCategoryAllocation)} to this category.";
            }

            return null;
        }

        private string? ValidateExpenseLimit(int projectId, int userId, string category, decimal expenseAmount, int? excludeExpenseId = null)
        {
            if (string.IsNullOrWhiteSpace(category))
                return "Please select a valid expense category.";
            if (expenseAmount <= 0)
                return "Expense amount must be greater than zero.";

            var project = _context.Projects.FirstOrDefault(p => p.ProjectId == projectId && p.UserId == userId);
            var totalSpentWithoutThisExpense = _expenseBll.GetTotalSpent(projectId);
            if (excludeExpenseId.HasValue)
            {
                var existingExpense = _expenseBll.GetExpenseById(excludeExpenseId.Value);
                if (existingExpense != null)
                {
                    totalSpentWithoutThisExpense -= existingExpense.Amount;
                }
            }

            if (project == null || project.TotalBudget <= 0)
                return "Please set the Total Project Budget before recording expenses.";

            var newTotalSpent = totalSpentWithoutThisExpense + expenseAmount;
            if (newTotalSpent > project.TotalBudget)
            {
                var remaining = Math.Max(0, project.TotalBudget - totalSpentWithoutThisExpense);
                return $"This expense would exceed the Total Project Budget. Remaining budget is {FormatPkr(remaining)}.";
            }

            var categoryBudget = GetCategoryAllocatedAmount(projectId, category);
            if (categoryBudget <= 0)
                return $"Please allocate a budget for {category} before recording expenses in this category.";

            var categorySpentWithoutThisExpense = GetCategorySpentAmount(projectId, userId, category, excludeExpenseId);
            var newCategorySpent = categorySpentWithoutThisExpense + expenseAmount;
            if (newCategorySpent > categoryBudget)
            {
                var remaining = Math.Max(0, categoryBudget - categorySpentWithoutThisExpense);
                return $"This expense would exceed the {category} budget. Remaining {category} budget is {FormatPkr(remaining)}.";
            }

            return null;
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

            item.Category = string.IsNullOrWhiteSpace(item.Category) ? "" : item.Category.Trim();
            var currentAllocated = _budgetBll.GetTotalBudget(projectId.Value);
            var categoryAllocated = GetCategoryAllocatedAmount(projectId.Value, item.Category) + item.Amount;
            var validationMessage = ValidateBudgetAllocation(projectId.Value, userId, item.Category, categoryAllocated, currentAllocated + item.Amount);
            if (validationMessage != null)
                return Json(new { success = false, message = validationMessage });

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
                if (amount <= 0)
                    return Json(new { success = false, message = "Total Project Budget must be greater than zero." });

                var totalSpent = _expenseBll.GetTotalSpent(projectId.Value);
                var totalAllocated = _budgetBll.GetTotalBudget(projectId.Value);
                if (amount < totalSpent)
                    return Json(new { success = false, message = $"Total Project Budget cannot be less than current expenses ({FormatPkr(totalSpent)})." });
                if (amount < totalAllocated)
                    return Json(new { success = false, message = $"Total Project Budget cannot be less than allocated category budgets ({FormatPkr(totalAllocated)})." });

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

            item.Category = string.IsNullOrWhiteSpace(item.Category) ? "" : item.Category.Trim();
            var projectId = existingBudget!.ProjectId!.Value;
            item.ProjectId = projectId;
            var currentAllocated = _budgetBll.GetTotalBudget(projectId);
            var newProjectAllocationTotal = currentAllocated - existingBudget.Amount + item.Amount;
            var newCategoryAllocation = GetCategoryAllocatedAmount(projectId, item.Category, item.BudgetId) + item.Amount;
            var validationMessage = ValidateBudgetAllocation(projectId, userId, item.Category, newCategoryAllocation, newProjectAllocationTotal);
            if (validationMessage != null)
                return Json(new { success = false, message = validationMessage });

            if (!string.Equals(existingBudget.Category, item.Category, StringComparison.OrdinalIgnoreCase))
            {
                var oldCategoryRemainingAllocation = GetCategoryAllocatedAmount(projectId, existingBudget.Category, item.BudgetId);
                var oldCategorySpent = GetCategorySpentAmount(projectId, userId, existingBudget.Category);
                if (oldCategorySpent > oldCategoryRemainingAllocation)
                {
                    return Json(new { success = false, message = $"Cannot move this allocation away from {existingBudget.Category}. That category already has {FormatPkr(oldCategorySpent)} in expenses and would be left with only {FormatPkr(oldCategoryRemainingAllocation)} allocated." });
                }
            }

            if (_budgetBll.UpdateBudget(item))
                return Json(new { success = true });
            return Json(new { success = false, message = "Invalid data" });
        }

        [HttpPost]
        public IActionResult DeleteBudget(int id)
        {
            int userId = GetCurrentUserId();
            if (!BudgetBelongsToUser(id, userId, out var budget))
                return Forbid();

            var spentInCategory = GetCategorySpentAmount(budget!.ProjectId!.Value, userId, budget.Category);
            if (spentInCategory > 0)
            {
                return Json(new { success = false, message = $"Cannot delete the {budget.Category} allocation because {FormatPkr(spentInCategory)} has already been spent in this category." });
            }

            if (_budgetBll.DeleteBudget(id))
                return Json(new { success = true });
            return Json(new { success = false, message = "Unable to delete budget allocation." });
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
            item.Category = string.IsNullOrWhiteSpace(item.Category) ? "" : item.Category.Trim();
            item.Description = string.IsNullOrWhiteSpace(item.Description) ? "" : item.Description.Trim();
            var validationMessage = ValidateExpenseLimit(projectId.Value, userId, item.Category, item.Amount);
            if (validationMessage != null)
                return Json(new { success = false, message = validationMessage });

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

            var projectId = existingExpense!.ProjectId!.Value;
            item.ProjectId = projectId;
            item.Category = string.IsNullOrWhiteSpace(item.Category) ? "" : item.Category.Trim();
            item.Description = string.IsNullOrWhiteSpace(item.Description) ? "" : item.Description.Trim();
            var validationMessage = ValidateExpenseLimit(projectId, userId, item.Category, item.Amount, item.ExpenseId);
            if (validationMessage != null)
                return Json(new { success = false, message = validationMessage });

            if (_expenseBll.UpdateExpense(item))
                return Json(new { success = true });
            return Json(new { success = false, message = "Invalid data" });
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
