using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class MaterialUsage
{
    public int UsageId { get; set; }

    public int PurchaseId { get; set; }

    public int PhaseId { get; set; }

    public decimal QuantityUsed { get; set; }

    public DateOnly UsageDate { get; set; }

    public string? Notes { get; set; }

    public virtual Phase Phase { get; set; } = null!;

    public virtual MaterialPurchase Purchase { get; set; } = null!;
}
