using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Phase
{
    public int PhaseId { get; set; }

    public int ProjectId { get; set; }
    public int? PropertyId { get; set; }
    public byte PhaseTypeId { get; set; }

    public string? CustomPhaseName { get; set; }
    public byte Sequence { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsCompleted { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual ICollection<MaterialUsage> MaterialUsages { get; set; } = new List<MaterialUsage>();

    public virtual PhaseType PhaseType { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual Property? Property { get; set; }

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
