using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class TaskStatus
{
    public byte StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
