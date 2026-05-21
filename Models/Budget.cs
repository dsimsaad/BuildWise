using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Budget
{
    public int BudgetId { get; set; }

    public int ProjectId { get; set; }

    public decimal TotalBudget { get; set; }

    public decimal? LaborBudget { get; set; }
    public decimal? MaterialBudget { get; set; }

    public decimal? MiscBudget { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Project Project { get; set; } = null!;
}
