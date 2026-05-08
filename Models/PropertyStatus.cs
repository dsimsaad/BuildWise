using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class PropertyStatus
{
    public byte StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}
