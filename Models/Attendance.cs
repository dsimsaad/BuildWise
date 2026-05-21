using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int WorkerId { get; set; }

    public int ProjectId { get; set; }

    public DateOnly AttendanceDate { get; set; }
    public byte StatusId { get; set; }
    public decimal WageForDay { get; set; }

    public string? Notes { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual AttendanceStatus Status { get; set; } = null!;

    public virtual Worker Worker { get; set; } = null!;
}
