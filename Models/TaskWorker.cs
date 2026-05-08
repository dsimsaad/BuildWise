using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class TaskWorker
{
    public int TaskWorkerId { get; set; }

    public int TaskId { get; set; }

    public int WorkerId { get; set; }

    public DateOnly AssignedDate { get; set; }

    public virtual Task Task { get; set; } = null!;

    public virtual Worker Worker { get; set; } = null!;
}
