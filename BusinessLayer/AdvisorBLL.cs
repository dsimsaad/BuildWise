using BuildWise.Models;
using BuildWise.DataLayer;

namespace BuildWise.BusinessLayer
{
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

            decimal allocatedBudget = projectId.HasValue
                ? budgetDal.GetTotalBudget(projectId)
                : (userId.HasValue ? budgetDal.GetTotalBudgetForUser(userId.Value) : 0);

            decimal approvedBudget = projectId.HasValue
                ? budgetDal.GetApprovedProjectBudget(projectId)
                : (userId.HasValue ? budgetDal.GetApprovedProjectBudgetForUser(userId.Value) : 0);

            decimal totalBudget = approvedBudget > 0 ? approvedBudget : allocatedBudget;
            decimal totalSpent = projectId.HasValue
                ? expenseDal.GetTotalExpenses(projectId)
                : (userId.HasValue ? expenseDal.GetTotalSpentForUser(userId.Value) : 0);

            decimal overallUsedPercent = totalBudget > 0 ? totalSpent / totalBudget * 100 : 0;
            List<BudgetItem> budgetItems = budgetDal.GetAll(projectId, userId);
            List<BudgetItem> expenseItems = expenseDal.GetExpensesByCategory(projectId, userId);

            if (totalBudget == 0)
            {
                results.Add(new AdvisorResult(
                    "No Budget Set",
                    "Warning",
                    "Warning: No total project budget is set, so spending cannot be measured against a project limit. Advice: Add a total project budget first, then add category allocations for material, labour, and other major cost heads.",
                    "General"));
            }
            else
            {
                if (totalSpent > totalBudget)
                {
                    var overAmount = totalSpent - totalBudget;
                    results.Add(new AdvisorResult(
                        "Overall Budget Exceeded",
                        "Warning",
                        $"Warning: Overall spending is {overallUsedPercent:0}% of the total budget and is over by PKR {overAmount:N0}. Advice: Stop non-essential purchases, review recent expenses, and increase the project budget only if the extra work is approved.",
                        "General"));
                }
                else if (totalSpent > totalBudget * 0.90m)
                {
                    var remaining = totalBudget - totalSpent;
                    results.Add(new AdvisorResult(
                        "Overall Budget Near Limit",
                        "Alert",
                        $"Warning: Overall spending is {overallUsedPercent:0}% of the total budget. Only PKR {remaining:N0} remains. Advice: Approve new purchases only after checking pending work and unpaid bills.",
                        "General"));
                }
                else if (totalSpent > totalBudget * 0.75m)
                {
                    var remaining = totalBudget - totalSpent;
                    results.Add(new AdvisorResult(
                        "Overall Budget Watch",
                        "Caution",
                        $"Warning: Overall spending is {overallUsedPercent:0}% of the total budget. PKR {remaining:N0} remains. Advice: Compare remaining budget with upcoming phases before committing large material orders.",
                        "General"));
                }
                else if (overallUsedPercent < 20m && totalSpent > 0)
                {
                    results.Add(new AdvisorResult(
                        "Overall Budget Healthy",
                        "Info",
                        $"Status: Overall spending is only {overallUsedPercent:0}% of the total budget. Advice: This is healthy unless construction progress is also low; check phase progress before assuming the project is behind.",
                        "General"));
                }
            }

            if (totalSpent == 0)
            {
                results.Add(new AdvisorResult(
                    "No Expenses",
                    "Info",
                    "Status: No expenses are recorded yet. Advice: Start logging material purchases, labour payments, and other expenses so the advisor can detect budget risks.",
                    "General"));
            }

            foreach (var b in budgetItems)
            {
                if (b.Amount <= 0) continue;

                var expense = expenseItems.FirstOrDefault(e => string.Equals(e.Category, b.Category, StringComparison.OrdinalIgnoreCase));
                decimal spentInCategory = expense?.Amount ?? 0;
                decimal categoryUsedPercent = spentInCategory / b.Amount * 100;

                if (spentInCategory > b.Amount)
                {
                    var overAmount = spentInCategory - b.Amount;
                    var overallContext = totalBudget > 0 && totalSpent <= totalBudget
                        ? $" Overall budget is still at {overallUsedPercent:0}% used, so this is a category allocation issue rather than a total budget overrun."
                        : "";

                    results.Add(new AdvisorResult(
                        "Category Allocation Exceeded",
                        "Alert",
                        $"Warning: {b.Category} has used {categoryUsedPercent:0}% of its allocated budget and is over by PKR {overAmount:N0}.{overallContext} Advice: Review recent {b.Category} expenses, pause new spending in this category if possible, or move budget from an underused category if the extra cost is expected.",
                        b.Category));
                }
                else if (spentInCategory > b.Amount * 0.80m)
                {
                    var remaining = b.Amount - spentInCategory;
                    results.Add(new AdvisorResult(
                        "Category Allocation Watch",
                        "Caution",
                        $"Warning: {b.Category} has used {categoryUsedPercent:0}% of its allocated budget. PKR {remaining:N0} remains in this category. Advice: Check upcoming {b.Category} needs before approving more expenses.",
                        b.Category));
                }
            }

            decimal labourSpent = expenseItems.FirstOrDefault(e => string.Equals(e.Category, "Labour", StringComparison.OrdinalIgnoreCase))?.Amount ?? 0;
            if (totalSpent > 0 && (labourSpent / totalSpent) > 0.40m)
            {
                var labourShare = labourSpent / totalSpent * 100;
                results.Add(new AdvisorResult(
                    "High Labour Share",
                    "Caution",
                    $"Warning: Labour is {labourShare:0}% of current spending. Advice: Review attendance, overtime, and idle days before adding more workers.",
                    "Labour"));
            }

            decimal miscSpent = expenseItems.FirstOrDefault(e => string.Equals(e.Category, "Miscellaneous", StringComparison.OrdinalIgnoreCase))?.Amount ?? 0;
            if (totalSpent > 0 && (miscSpent / totalSpent) > 0.15m)
            {
                var miscShare = miscSpent / totalSpent * 100;
                results.Add(new AdvisorResult(
                    "High Miscellaneous Share",
                    "Caution",
                    $"Warning: Miscellaneous expenses are {miscShare:0}% of current spending. Advice: Reclassify repeated costs into proper categories so budget tracking stays clear.",
                    "Miscellaneous"));
            }

            return results;
        }
    }
}
