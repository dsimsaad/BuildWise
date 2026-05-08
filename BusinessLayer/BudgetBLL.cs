using BuildWise.Models;
using BuildWise.DataLayer;

namespace BuildWise.BusinessLayer
{
    /// <summary>
    /// Business Logic Layer for Budget management
    /// </summary>
    public class BudgetBLL
    {
        private BudgetDAL dal;

        public BudgetBLL(string connectionString)
        {
            dal = new BudgetDAL(connectionString);
        }

        public List<BudgetItem> GetAllBudgets()
        {
            return dal.GetAll();
        }

        public BudgetItem GetBudgetById(int id)
        {
            return dal.GetById(id);
        }

        public bool AddBudget(BudgetItem item)
        {
            // Simple validation: Category must not be empty, Amount must be positive
            if (string.IsNullOrWhiteSpace(item.Category) || item.Amount <= 0)
            {
                return false;
            }
            return dal.Add(item);
        }

        public bool UpdateBudget(BudgetItem item)
        {
            if (item.BudgetId <= 0 || string.IsNullOrWhiteSpace(item.Category) || item.Amount <= 0)
            {
                return false;
            }
            return dal.Update(item);
        }

        public bool DeleteBudget(int id)
        {
            if (id <= 0) return false;
            return dal.Delete(id);
        }

        public decimal GetTotalBudget()
        {
            return dal.GetTotalBudget();
        }
    }
}
