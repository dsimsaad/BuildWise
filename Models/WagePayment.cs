using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class WagePayment
{
    /// <summary>
    /// Auto-increment PK
    /// </summary>
    public int WagePaymentId { get; set; }

    public int WorkerId { get; set; }

    public int ProjectId { get; set; }

    public decimal AmountPaid { get; set; }

    public DateOnly PaymentDate { get; set; }

    public byte? PaymentMethodId { get; set; }

    /// <summary>
    /// Start date of the pay period being settled
    /// </summary>
    public DateOnly? PeriodFrom { get; set; }

    /// <summary>
    /// End date of the pay period being settled
    /// </summary>
    public DateOnly? PeriodTo { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual Worker Worker { get; set; } = null!;
}
