using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class AreaUnit
{
    public byte UnitId { get; set; }

    public string UnitName { get; set; } = null!;

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}
