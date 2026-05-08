using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Phase
{
    /// <summary>
    /// Auto-increment PK
    /// </summary>
    public int PhaseId { get; set; }

    public int ProjectId { get; set; }

    /// <summary>
    /// FK to PhaseType — Foundation, Grey Structure, Finishing etc
    /// </summary>
    public byte PhaseTypeId { get; set; }

    /// <summary>
    /// Used only when PhaseTypeID = 8 (Custom)
    /// </summary>
    public string? CustomPhaseName { get; set; }

    /// <summary>
    /// Ordering of phases within the project (1 = first)
    /// </summary>
    public byte Sequence { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCompleted { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual ICollection<MaterialUsage> MaterialUsages { get; set; } = new List<MaterialUsage>();

    public virtual PhaseType PhaseType { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
