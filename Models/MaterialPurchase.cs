using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class MaterialPurchase
{
    public int PurchaseId { get; set; }

    public int ProjectId { get; set; }

    public int MaterialId { get; set; }

    public int? SupplierId { get; set; }

    public decimal Quantity { get; set; }

    public byte UnitId { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal? TotalCost { get; set; }

    public DateOnly PurchaseDate { get; set; }

    public string? InvoiceNumber { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Material Material { get; set; } = null!;

    public virtual ICollection<MaterialUsage> MaterialUsages { get; set; } = new List<MaterialUsage>();

    public virtual Project Project { get; set; } = null!;

    public virtual Supplier? Supplier { get; set; }

    public virtual MaterialUnit Unit { get; set; } = null!;
}
