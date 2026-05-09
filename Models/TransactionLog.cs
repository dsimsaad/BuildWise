namespace BuildWise.Models
{
    /// <summary>
    /// Automatic transaction log entry - created whenever an expense is added/edited/deleted
    /// </summary>
    public class TransactionLog
    {
        public int TransactionId { get; set; }
        public int? ProjectId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; }  // Added, Updated, Deleted
        public string Category { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public decimal BudgetEffect { get; set; }  // percentage of total budget

        public TransactionLog()
        {
            TransactionDate = DateTime.Now;
            TransactionType = "";
            Category = "";
            Description = "";
            Amount = 0;
            BudgetEffect = 0;
        }
    }
}
