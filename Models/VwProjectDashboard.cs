using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class VwProjectDashboard
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = null!;

    public string PropertyName { get; set; } = null!;

    public string PropertyLocation { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? ExpectedEndDate { get; set; }

    public decimal TotalBudget { get; set; }

    public decimal TotalExpenses { get; set; }

    public decimal TotalMaterials { get; set; }

    public decimal TotalWagesPaid { get; set; }

    public decimal? TotalSpent { get; set; }

    public decimal? RemainingBudget { get; set; }

    public decimal TotalClientPayments { get; set; }

    public decimal? ProfitLoss { get; set; }

    public int? TotalPhases { get; set; }

    public int? CompletedPhases { get; set; }

    public double? PhaseProgressPct { get; set; }

    public int? TotalTasks { get; set; }

    public int? CompletedTasks { get; set; }

    public double? TaskProgressPct { get; set; }

    public bool IsCompleted { get; set; }

    public string OwnerName { get; set; } = null!;
}
