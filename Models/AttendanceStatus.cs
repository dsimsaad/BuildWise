using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class AttendanceStatus
{
    public byte StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
