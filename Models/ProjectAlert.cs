using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class ProjectAlert
{
    public int AlertId { get; set; }

    public int ProjectId { get; set; }

    public string AlertType { get; set; } = null!;

    public string? AlertMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }

    public virtual Project Project { get; set; } = null!;
}
