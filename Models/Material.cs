using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Material
{
    public int MaterialId { get; set; }

    public string MaterialName { get; set; } = null!;

    public byte DefaultUnitId { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public virtual MaterialUnit DefaultUnit { get; set; } = null!;

    public virtual ICollection<MaterialPurchase> MaterialPurchases { get; set; } = new List<MaterialPurchase>();
}
