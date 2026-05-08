using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class PropertyType
{
    public byte TypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}
