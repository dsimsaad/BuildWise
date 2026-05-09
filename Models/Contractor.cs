using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Contractor
{
    public int ContractorId { get; set; }

    public int? UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Email { get; set; }

    public string? SpecialityNotes { get; set; }

    public decimal? ContractCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual ICollection<Worker> Workers { get; set; } = new List<Worker>();
}
