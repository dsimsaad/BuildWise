using BuildWise.Models;
using BuildWise.DataLayer;

namespace BuildWise.BusinessLayer
{
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
            if (string.IsNullOrWhiteSpace(item.Category) || item.Amount <= 0)
            {
                return false;
            }

            bool success = expenseDal.Add(item);
            if (success)
            {
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

        public bool ApplyMaterialReturn(MaterialPurchase purchase, decimal returnQuantity, decimal returnAmount, string? returnNotes = null)
        {
            if (purchase.ProjectId <= 0 || returnQuantity <= 0 || returnAmount <= 0)
            {
                return false;
            }

            var adjustedExpense = true;
            var mirroredExpense = expenseDal.GetMaterialPurchaseExpense(purchase.ProjectId, purchase.PurchaseId);
            if (mirroredExpense != null)
            {
                var newAmount = Math.Round((purchase.TotalCost ?? purchase.Quantity * purchase.UnitPrice), 2);
                adjustedExpense = newAmount > 0
                    ? expenseDal.UpdateAmount(mirroredExpense.ExpenseId, newAmount)
                    : expenseDal.Delete(mirroredExpense.ExpenseId);
            }

            return adjustedExpense && LogMaterialReturnTransaction(purchase, returnQuantity, returnAmount, returnNotes);
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

        private bool LogMaterialReturnTransaction(MaterialPurchase purchase, decimal returnQuantity, decimal returnAmount, string? returnNotes)
        {
            decimal totalBudget = budgetDal.GetTotalBudget(purchase.ProjectId);
            decimal effect = 0;
            if (totalBudget > 0)
            {
                effect = -((returnAmount / totalBudget) * 100);
            }

            var materialName = purchase.Material?.MaterialName ?? "material";
            var unitName = purchase.Unit?.UnitName ?? "units";
            var description = $"Returned {returnQuantity:0.###} {unitName} of {materialName} from purchase #{purchase.PurchaseId}. Expense reduced by PKR {returnAmount:N0}.";
            if (!string.IsNullOrWhiteSpace(returnNotes))
            {
                description += $" Notes: {returnNotes.Trim()}";
            }

            TransactionLog log = new TransactionLog
            {
                ProjectId = purchase.ProjectId,
                TransactionType = "Returned",
                Category = "Material",
                Description = description,
                Amount = returnAmount,
                BudgetEffect = Math.Round(effect, 2)
            };
            return transactionDal.Add(log);
        }
    }
}
