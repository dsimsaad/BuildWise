using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class VwPhaseWiseCost
{
    public int PhaseId { get; set; }

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = null!;

    public string PhaseName { get; set; } = null!;

    public string DisplayPhaseName { get; set; } = null!;

    public byte Sequence { get; set; }

    public bool IsCompleted { get; set; }

    public decimal ExpenseCost { get; set; }

    public decimal MaterialCost { get; set; }

    public decimal? TotalPhaseCost { get; set; }

    public int? TotalTasks { get; set; }

    public int? CompletedTasks { get; set; }

    public int? PendingTasks { get; set; }
}
