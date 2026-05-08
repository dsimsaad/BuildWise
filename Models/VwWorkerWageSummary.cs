using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class VwWorkerWageSummary
{
    public int WorkerId { get; set; }

    public string WorkerName { get; set; } = null!;

    public string? SkillType { get; set; }

    public decimal DailyWage { get; set; }

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = null!;

    public int? DaysPresent { get; set; }

    public int? DaysAbsent { get; set; }

    public int? HalfDays { get; set; }

    public decimal? TotalWageEarned { get; set; }

    public decimal TotalWagePaid { get; set; }

    public decimal? WageDue { get; set; }
}
