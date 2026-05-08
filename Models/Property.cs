using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Property
{
    /// <summary>
    /// Auto-increment PK
    /// </summary>
    public int PropertyId { get; set; }

    /// <summary>
    /// FK to Users — owner of this property
    /// </summary>
    public int UserId { get; set; }

    public string PropertyName { get; set; } = null!;

    /// <summary>
    /// FK to PropertyType: Plot/House/Apartment/Commercial
    /// </summary>
    public byte TypeId { get; set; }

    /// <summary>
    /// FK to PropertyStatus: Under Construction/Completed etc
    /// </summary>
    public byte StatusId { get; set; }

    public string Location { get; set; } = null!;

    public string? City { get; set; }

    /// <summary>
    /// Numeric area value, unit determined by AreaUnitID
    /// </summary>
    public decimal AreaSize { get; set; }

    /// <summary>
    /// FK to AreaUnit: Marla/Kanal/SqFt/SqM
    /// </summary>
    public byte AreaUnitId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual AreaUnit AreaUnit { get; set; } = null!;

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual PropertyStatus Status { get; set; } = null!;

    public virtual PropertyType Type { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
