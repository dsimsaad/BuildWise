namespace BuildWise.Models
{
    public class ExpenseItem
    {
        public int ExpenseId { get; set; }
        public int? ProjectId { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ExpenseItem()
        {
            Category = "";
            Description = "";
            Amount = 0;
            ExpenseDate = DateTime.Now;
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }
    }
}
