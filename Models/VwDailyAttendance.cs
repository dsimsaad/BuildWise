using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class VwDailyAttendance
{
    public DateOnly AttendanceDate { get; set; }

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = null!;

    public int WorkerId { get; set; }

    public string WorkerName { get; set; } = null!;

    public string? SkillType { get; set; }

    public string AttendanceStatus { get; set; } = null!;

    public decimal WageForDay { get; set; }

    public string? Notes { get; set; }
}
