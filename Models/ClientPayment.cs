using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class ClientPayment
{
    public int PaymentId { get; set; }

    public int ProjectId { get; set; }
    public decimal Amount { get; set; }

    public DateOnly PaymentDate { get; set; }

    public byte? PaymentMethodId { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual Project Project { get; set; } = null!;
}
