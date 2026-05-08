using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Worker
{
    /// <summary>
    /// Auto-increment PK
    /// </summary>
    public int WorkerId { get; set; }

    /// <summary>
    /// FK to Contractors — null if independent worker
    /// </summary>
    public int? ContractorId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    /// <summary>
    /// Pakistani CNIC number (13 digits + dashes), unique identifier
    /// </summary>
    public string? Cnic { get; set; }

    /// <summary>
    /// Default daily wage in PKR, can be overridden per attendance record
    /// </summary>
    public decimal DailyWage { get; set; }

    /// <summary>
    /// e.g. Mason, Carpenter, Electrician, Helper, Plumber
    /// </summary>
    public string? SkillType { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Contractor? Contractor { get; set; }

    public virtual ICollection<TaskWorker> TaskWorkers { get; set; } = new List<TaskWorker>();

    public virtual ICollection<WagePayment> WagePayments { get; set; } = new List<WagePayment>();
}
