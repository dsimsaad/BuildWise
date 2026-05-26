using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Property
{
    public int PropertyId { get; set; }
    public int UserId { get; set; }
    public int? ProjectId { get; set; }

    public string PropertyName { get; set; } = null!;
    public byte TypeId { get; set; }
    public byte StatusId { get; set; }

    public string Location { get; set; } = null!;

    public string? City { get; set; }
    public decimal AreaSize { get; set; }
    public byte AreaUnitId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual AreaUnit AreaUnit { get; set; } = null!;

    public virtual Project? Project { get; set; }

    public virtual ICollection<Phase> Phases { get; set; } = new List<Phase>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual PropertyStatus Status { get; set; } = null!;

    public virtual PropertyType Type { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
