namespace BuildWise.Models
{
    public class PhaseTask
    {
        public int TaskId { get; set; }
        public int PhaseId { get; set; }
        public string TaskName { get; set; }
        public string Status { get; set; }    
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Weight { get; set; }   
        public DateTime CreatedAt { get; set; }

        public PhaseTask()
        {
            TaskName = "";
            Status = "Pending";
            Weight = 0;
            CreatedAt = DateTime.Now;
        }
    }
}
