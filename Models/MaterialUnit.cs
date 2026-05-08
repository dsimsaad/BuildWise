using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class MaterialUnit
{
    public byte UnitId { get; set; }

    public string UnitName { get; set; } = null!;

    public virtual ICollection<MaterialPurchase> MaterialPurchases { get; set; } = new List<MaterialPurchase>();

    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
}
