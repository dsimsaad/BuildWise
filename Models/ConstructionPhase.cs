namespace BuildWise.Models
{
    /// <summary>
    /// Represents a construction phase (e.g., Foundation, Structure)
    /// </summary>
    public class ConstructionPhase
    {
        public int PhaseId { get; set; }
        public int? ProjectId { get; set; }
        public string PhaseName { get; set; }
        public decimal Weight { get; set; }      // importance percentage
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PhaseTask> Tasks { get; set; }  // child tasks
        public decimal Progress { get; set; }        // calculated field

        public ConstructionPhase()
        {
            PhaseName = "";
            Weight = 0;
            SortOrder = 0;
            CreatedAt = DateTime.Now;
            Tasks = new List<PhaseTask>();
            Progress = 0;
        }
    }
}
