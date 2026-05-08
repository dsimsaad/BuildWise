using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Budget
{
    /// <summary>
    /// Auto-increment PK — one budget record per project
    /// </summary>
    public int BudgetId { get; set; }

    public int ProjectId { get; set; }

    public decimal TotalBudget { get; set; }

    /// <summary>
    /// Portion of budget allocated to labor costs
    /// </summary>
    public decimal? LaborBudget { get; set; }

    /// <summary>
    /// Portion allocated to materials
    /// </summary>
    public decimal? MaterialBudget { get; set; }

    /// <summary>
    /// Remaining allocation for equipment, transport, misc
    /// </summary>
    public decimal? MiscBudget { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Project Project { get; set; } = null!;
}
