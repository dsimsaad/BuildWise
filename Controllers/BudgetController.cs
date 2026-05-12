using Microsoft.AspNetCore.Mvc;
using BuildWise.Models;
using BuildWise.BusinessLayer;
using Microsoft.AspNetCore.Authorization;

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

        [HttpGet]
        public IActionResult GetData()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

            var selectedProjectId = GetSelectedProjectId();
            var budgets = _budgetBll.GetAllBudgets(selectedProjectId, userId);
            var expenses = _expenseBll.GetAllExpenses(selectedProjectId, userId);
            var totalBudget = selectedProjectId.HasValue 
                ? _budgetBll.GetTotalBudget(selectedProjectId) 
                : _budgetBll.GetTotalBudgetForUser(userId);
            var totalSpent = selectedProjectId.HasValue 
                ? _expenseBll.GetTotalSpent(selectedProjectId) 
                : _expenseBll.GetTotalSpentForUser(userId);
            
            var categoryExpenses = _expenseBll.GetExpensesByCategory(selectedProjectId); // Needs user filtering too if null

            var project = selectedProjectId.HasValue ? _context.Projects.Find(selectedProjectId.Value) : null;
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
            var projectId = GetSelectedProjectId();
            if (projectId == null)
                return Json(new { success = false, message = "Please select a project first." });

            var project = _context.Projects.Find(projectId.Value);
            var currentAllocated = _budgetBll.GetTotalBudget(projectId.Value);
            
            if (project != null && project.TotalBudget > 0 && (currentAllocated + item.Amount) > project.TotalBudget)
            {
                return Json(new { success = false, message = $"Category budget exceeds the Total Project Budget (RS. {project.TotalBudget}). Please increase the Total Budget first." });
            }

            item.ProjectId = projectId;
            if (_budgetBll.AddBudget(item))
                return Json(new { success = true });
            return Json(new { success = false, message = "Invalid data" });
        }

        [HttpPost]
        public IActionResult UpdateTotalProjectBudget([FromBody] decimal amount)
        {
            var projectId = GetSelectedProjectId();
            if (projectId == null)
                return Json(new { success = false, message = "Please select a project first." });

            var project = _context.Projects.Find(projectId.Value);
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
            if (_budgetBll.UpdateBudget(item))
                return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult DeleteBudget(int id)
        {
            if (_budgetBll.DeleteBudget(id))
                return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult AddExpense([FromBody] ExpenseItem item)
        {
            var projectId = GetSelectedProjectId();
            if (projectId == null)
                return Json(new { success = false, message = "Please select a project first." });

            item.ProjectId = projectId;
            if (_expenseBll.AddExpense(item))
                return Json(new { success = true });
            return Json(new { success = false, message = "Invalid data" });
        }

        [HttpPost]
        public IActionResult UpdateExpense([FromBody] ExpenseItem item)
        {
            if (_expenseBll.UpdateExpense(item))
                return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult DeleteExpense(int id)
        {
            if (_expenseBll.DeleteExpense(id))
                return Json(new { success = true });
            return Json(new { success = false });
        }
    }
}
