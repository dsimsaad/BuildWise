using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Task
{
    public int TaskId { get; set; }

    public int PhaseId { get; set; }

    public int? ContractorId { get; set; }

    public string TaskName { get; set; } = null!;

    public string? Description { get; set; }

    public byte StatusId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
    public decimal? EstimatedCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Contractor? Contractor { get; set; }

    public virtual Phase Phase { get; set; } = null!;

    public virtual TaskStatus Status { get; set; } = null!;

    public virtual ICollection<TaskWorker> TaskWorkers { get; set; } = new List<TaskWorker>();
}
