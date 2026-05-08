using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Attendance
{
    /// <summary>
    /// Auto-increment PK
    /// </summary>
    public int AttendanceId { get; set; }

    /// <summary>
    /// FK to Workers
    /// </summary>
    public int WorkerId { get; set; }

    /// <summary>
    /// FK to Projects — which site they attended
    /// </summary>
    public int ProjectId { get; set; }

    public DateOnly AttendanceDate { get; set; }

    /// <summary>
    /// FK to AttendanceStatus: Present/Absent/Half Day/Leave
    /// </summary>
    public byte StatusId { get; set; }

    /// <summary>
    /// Actual wage paid for this day (0 if absent, 50% if half day)
    /// </summary>
    public decimal WageForDay { get; set; }

    public string? Notes { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual AttendanceStatus Status { get; set; } = null!;

    public virtual Worker Worker { get; set; } = null!;
}
