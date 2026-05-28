using BuildWise.Models;
using BuildWise.DataLayer;

namespace BuildWise.BusinessLayer
{
    public class BudgetBLL
    {
        private BudgetDAL dal;
        private TransactionDAL transactionDal;

        public BudgetBLL(string connectionString)
        {
            dal = new BudgetDAL(connectionString);
            transactionDal = new TransactionDAL(connectionString);
        }

        public List<BudgetItem> GetAllBudgets(int? projectId = null, int? userId = null)
        {
            return dal.GetAll(projectId, userId);
        }

        public BudgetItem? GetBudgetById(int id)
        {
            return dal.GetById(id);
        }

        public bool AddBudget(BudgetItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Category) || item.Amount <= 0)
            {
                return false;
            }
            bool success = dal.Add(item);
            if (success)
            {
                LogTransaction("Added", item, "Budget allocation added.");
            }
            return success;
        }

        public bool UpdateBudget(BudgetItem item)
        {
            if (item.BudgetId <= 0 || string.IsNullOrWhiteSpace(item.Category) || item.Amount <= 0)
            {
                return false;
            }
            bool success = dal.Update(item);
            if (success)
            {
                LogTransaction("Updated", item, "Budget allocation updated.");
            }
            return success;
        }

        public bool DeleteBudget(int id)
        {
            if (id <= 0) return false;
            var item = dal.GetById(id);
            bool success = dal.Delete(id);
            if (success && item != null)
            {
                LogTransaction("Deleted", item, "Budget allocation deleted.");
            }
            return success;
        }

        public decimal GetTotalBudget(int? projectId = null)
        {
            return dal.GetTotalBudget(projectId);
        }

        public decimal GetTotalBudgetForUser(int userId)
        {
            return dal.GetTotalBudgetForUser(userId);
        }

        private void LogTransaction(string type, BudgetItem item, string description)
        {
            TransactionLog log = new TransactionLog
            {
                ProjectId = item.ProjectId,
                TransactionType = type,
                Category = item.Category,
                Description = description,
                Amount = item.Amount,
                BudgetEffect = 0
            };

            transactionDal.Add(log);
        }
    }
}
