using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class WagePayment
{
    public int WagePaymentId { get; set; }

    public int WorkerId { get; set; }

    public int ProjectId { get; set; }

    public decimal AmountPaid { get; set; }

    public DateOnly PaymentDate { get; set; }

    public byte? PaymentMethodId { get; set; }
    public DateOnly? PeriodFrom { get; set; }

    public DateOnly? PeriodTo { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual Worker Worker { get; set; } = null!;
}
