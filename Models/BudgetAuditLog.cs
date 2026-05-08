using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class BudgetAuditLog
{
    public int LogId { get; set; }

    public int BudgetId { get; set; }

    public int ProjectId { get; set; }

    public decimal? OldBudget { get; set; }

    public decimal? NewBudget { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? ChangedByMsg { get; set; }
}
