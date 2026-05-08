using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class PhaseType
{
    public byte PhaseTypeId { get; set; }

    public string PhaseName { get; set; } = null!;

    public virtual ICollection<Phase> Phases { get; set; } = new List<Phase>();
}
