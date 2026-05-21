using BuildWise.Models;
using BuildWise.DataLayer;

namespace BuildWise.BusinessLayer
{
    /// <summary>
    /// Business Logic Layer for Expense management
    /// </summary>
    public class ExpenseBLL
    {
        private ExpenseDAL expenseDal;
        private TransactionDAL transactionDal;
        private BudgetDAL budgetDal;

        public ExpenseBLL(string connectionString)
        {
            expenseDal = new ExpenseDAL(connectionString);
            transactionDal = new TransactionDAL(connectionString);
            budgetDal = new BudgetDAL(connectionString);
        }

        public List<ExpenseItem> GetAllExpenses(int? projectId = null, int? userId = null)
        {
            return expenseDal.GetAll(projectId, userId);
        }

        public ExpenseItem? GetExpenseById(int id)
        {
            return expenseDal.GetById(id);
        }

        public bool AddExpense(ExpenseItem item)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(item.Category) || item.Amount <= 0)
            {
                return false;
            }

            bool success = expenseDal.Add(item);
            if (success)
            {
                // Auto-log transaction
                LogTransaction("Added", item);
            }
            return success;
        }

        public bool UpdateExpense(ExpenseItem item)
        {
            if (item.ExpenseId <= 0 || string.IsNullOrWhiteSpace(item.Category) || item.Amount <= 0)
            {
                return false;
            }

            bool success = expenseDal.Update(item);
            if (success)
            {
                LogTransaction("Updated", item);
            }
            return success;
        }

        public bool DeleteExpense(int id)
        {
            if (id <= 0) return false;

            ExpenseItem? item = expenseDal.GetById(id);
            if (item == null) return false;

            bool success = expenseDal.Delete(id);
            if (success)
            {
                LogTransaction("Deleted", item);
            }
            return success;
        }

        public decimal GetTotalSpent(int? projectId = null)
        {
            return expenseDal.GetTotalExpenses(projectId);
        }

        public decimal GetTotalSpentForUser(int userId)
        {
            return expenseDal.GetTotalSpentForUser(userId);
        }

        public List<BudgetItem> GetExpensesByCategory(int? projectId = null, int? userId = null)
        {
            return expenseDal.GetExpensesByCategory(projectId, userId);
        }

        private void LogTransaction(string type, ExpenseItem item)
        {
            decimal totalBudget = budgetDal.GetTotalBudget(item.ProjectId);
            decimal effect = 0;
            if (totalBudget > 0)
            {
                effect = (item.Amount / totalBudget) * 100;
            }

            TransactionLog log = new TransactionLog
            {
                ProjectId = item.ProjectId,
                TransactionType = type,
                Category = item.Category,
                Description = item.Description,
                Amount = item.Amount,
                BudgetEffect = Math.Round(effect, 2)
            };
            transactionDal.Add(log);
        }
    }
}
