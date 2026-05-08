namespace BuildWise.Models
{
    /// <summary>
    /// Represents a task within a construction phase
    /// </summary>
    public class PhaseTask
    {
        public int TaskId { get; set; }
        public int PhaseId { get; set; }
        public string TaskName { get; set; }
        public string Status { get; set; }    // Pending, InProgress, Completed, Delayed
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Weight { get; set; }   // weight within the phase
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
