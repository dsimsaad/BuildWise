namespace BuildWise.Models
{
    public class ConstructionPhase
    {
        public int PhaseId { get; set; }
        public int? ProjectId { get; set; }
        public string PhaseName { get; set; }
        public decimal Weight { get; set; }      
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PhaseTask> Tasks { get; set; }  
        public decimal Progress { get; set; }       

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
