using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Worker
{
    public int WorkerId { get; set; }
    
    public int? UserId { get; set; }

    public int? ProjectId { get; set; }

    public int? ContractorId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Cnic { get; set; }

    public decimal DailyWage { get; set; }

    public string? SkillType { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Contractor? Contractor { get; set; }

    public virtual Project? Project { get; set; }

    public virtual ICollection<WorkerProjectAssignment> WorkerProjectAssignments { get; set; } = new List<WorkerProjectAssignment>();

    public virtual ICollection<TaskWorker> TaskWorkers { get; set; } = new List<TaskWorker>();

    public virtual ICollection<WagePayment> WagePayments { get; set; } = new List<WagePayment>();
}
