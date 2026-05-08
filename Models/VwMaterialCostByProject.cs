using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class VwMaterialCostByProject
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = null!;

    public string MaterialName { get; set; } = null!;

    public decimal? TotalQuantityPurchased { get; set; }

    public string UnitName { get; set; } = null!;

    public decimal? TotalCost { get; set; }

    public string? SupplierName { get; set; }
}
