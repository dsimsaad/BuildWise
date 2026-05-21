namespace BuildWise.Models
{
    public class BudgetItem
    {
        public int BudgetId { get; set; }
        public int? ProjectId { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public BudgetItem()
        {
            Category = "";
            Amount = 0;
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }
    }
}
