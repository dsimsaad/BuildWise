using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Task
{
    /// <summary>
    /// Auto-increment PK
    /// </summary>
    public int TaskId { get; set; }

    /// <summary>
    /// FK to Phases — task belongs to this phase
    /// </summary>
    public int PhaseId { get; set; }

    /// <summary>
    /// FK to Contractors — nullable, who is responsible
    /// </summary>
    public int? ContractorId { get; set; }

    public string TaskName { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// FK to TaskStatus: Pending/In Progress/Completed/Hold/Cancelled
    /// </summary>
    public byte StatusId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Budgeted cost for this specific task in PKR
    /// </summary>
    public decimal? EstimatedCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Contractor? Contractor { get; set; }

    public virtual Phase Phase { get; set; } = null!;

    public virtual TaskStatus Status { get; set; } = null!;

    public virtual ICollection<TaskWorker> TaskWorkers { get; set; } = new List<TaskWorker>();
}
