using BuildWise.Models;
using BuildWise.DataLayer;

namespace BuildWise.BusinessLayer
{
    /// <summary>
    /// Cost Advisor - Rule-based expert system logic
    /// </summary>
    public class AdvisorBLL
    {
        private BudgetDAL budgetDal;
        private ExpenseDAL expenseDal;

        public AdvisorBLL(string connectionString)
        {
            budgetDal = new BudgetDAL(connectionString);
            expenseDal = new ExpenseDAL(connectionString);
        }

        public List<AdvisorResult> GetAnalysis(int? projectId = null, int? userId = null)
        {
            List<AdvisorResult> results = new List<AdvisorResult>();
            
            decimal totalBudget = (projectId.HasValue) 
                ? budgetDal.GetTotalBudget(projectId) 
                : (userId.HasValue ? budgetDal.GetTotalBudgetForUser(userId.Value) : 0);
                
            decimal totalSpent = (projectId.HasValue) 
                ? expenseDal.GetTotalExpenses(projectId) 
                : (userId.HasValue ? expenseDal.GetTotalSpentForUser(userId.Value) : 0);
                
            List<BudgetItem> budgetItems = budgetDal.GetAll(projectId, userId);
            List<BudgetItem> expenseItems = expenseDal.GetExpensesByCategory(projectId);

            // 1. Total Budget vs Total Spent Rules
            if (totalBudget == 0)
            {
                results.Add(new AdvisorResult("No Budget Set", "Warning", "No budget has been set. Please set a budget to track expenses.", "General"));
            }
            else
            {
                if (totalSpent > totalBudget)
                {
                    results.Add(new AdvisorResult("Budget Exceeded", "Warning", "You have exceeded your total budget. Immediate action required.", "General"));
                }
                else if (totalSpent > totalBudget * 0.90m)
                {
                    results.Add(new AdvisorResult("Budget Alert", "Alert", "You are very close to your budget limit (90%+ spent).", "General"));
                }
                else if (totalSpent > totalBudget * 0.75m)
                {
                    results.Add(new AdvisorResult("Budget Caution", "Caution", "You have used more than 75% of your budget.", "General"));
                }
                else if (totalSpent < totalBudget * 0.20m && totalSpent > 0)
                {
                    results.Add(new AdvisorResult("Budget Info", "Info", "Only 20% of your budget is used. Project may be behind schedule.", "General"));
                }
            }

            if (totalSpent == 0)
            {
                results.Add(new AdvisorResult("No Expenses", "Info", "No expenses recorded yet. Start adding expenses to get insights.", "General"));
            }

            // 2. Category specific rules
            foreach (var b in budgetItems)
            {
                var expense = expenseItems.FirstOrDefault(e => e.Category == b.Category);
                decimal spentInCategory = expense?.Amount ?? 0;

                if (spentInCategory > b.Amount)
                {
                    results.Add(new AdvisorResult("Category Overspent", "Warning", $"Category {b.Category} has exceeded its allocated budget.", b.Category));
                }
                else if (spentInCategory > b.Amount * 0.80m)
                {
                    results.Add(new AdvisorResult("Category Limit", "Alert", $"Category {b.Category} is approaching its budget limit.", b.Category));
                }
            }

            // 3. Labour & Miscellaneous Ratio Rules
            decimal labourSpent = expenseItems.FirstOrDefault(e => e.Category == "Labour")?.Amount ?? 0;
            if (totalSpent > 0 && (labourSpent / totalSpent) > 0.40m)
            {
                results.Add(new AdvisorResult("High Labour Cost", "Caution", "Labour costs are high — consider reviewing workforce expenses.", "Labour"));
            }

            decimal miscSpent = expenseItems.FirstOrDefault(e => e.Category == "Miscellaneous")?.Amount ?? 0;
            if (totalSpent > 0 && (miscSpent / totalSpent) > 0.15m)
            {
                results.Add(new AdvisorResult("High Misc Cost", "Caution", "Miscellaneous expenses are unusually high. Review untracked costs.", "Miscellaneous"));
            }

            return results;
        }
    }
}
