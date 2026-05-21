namespace BuildWise.Models;

public partial class WorkerProjectAssignment
{
    public int WorkerProjectAssignmentId { get; set; }

    public int WorkerId { get; set; }

    public int ProjectId { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual Worker Worker { get; set; } = null!;
}
