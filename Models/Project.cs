using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Project
{
    /// <summary>
    /// Auto-increment PK
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// FK to Properties — which property this project is on
    /// </summary>
    public int PropertyId { get; set; }

    public int UserId { get; set; }

    public string ProjectName { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? ExpectedEndDate { get; set; }

    /// <summary>
    /// Filled when project is marked complete
    /// </summary>
    public DateOnly? ActualEndDate { get; set; }

    /// <summary>
    /// Overall approved budget in PKR
    /// </summary>
    public decimal TotalBudget { get; set; }

    /// <summary>
    /// 1 = project closed, 0 = ongoing
    /// </summary>
    public bool IsCompleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Budget? Budget { get; set; }

    public virtual ICollection<ClientPayment> ClientPayments { get; set; } = new List<ClientPayment>();

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual ICollection<MaterialPurchase> MaterialPurchases { get; set; } = new List<MaterialPurchase>();

    public virtual ICollection<Phase> Phases { get; set; } = new List<Phase>();

    public virtual ICollection<ProjectAlert> ProjectAlerts { get; set; } = new List<ProjectAlert>();

    public virtual Property Property { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual ICollection<WagePayment> WagePayments { get; set; } = new List<WagePayment>();
}
