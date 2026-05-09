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

        public BudgetController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BuildWise") ?? "";
            _budgetBll = new BudgetBLL(_connectionString);
            _expenseBll = new ExpenseBLL(_connectionString);
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetData()
        {
            var budgets = _budgetBll.GetAllBudgets();
            var expenses = _expenseBll.GetAllExpenses();
            var totalBudget = _budgetBll.GetTotalBudget();
            var totalSpent = _expenseBll.GetTotalSpent();
            var categoryExpenses = _expenseBll.GetExpensesByCategory();

            return Json(new { 
                budgets, 
                expenses, 
                totalBudget, 
                totalSpent,
                categoryExpenses
            });
        }

        [HttpPost]
        public IActionResult AddBudget([FromBody] BudgetItem item)
        {
            if (_budgetBll.AddBudget(item))
                return Json(new { success = true });
            return Json(new { success = false, message = "Invalid data" });
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
