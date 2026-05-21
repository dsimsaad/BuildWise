using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Expense
{
    public int ExpenseId { get; set; }

    public int ProjectId { get; set; }

    public int? PhaseId { get; set; }

    public byte CategoryId { get; set; }

    public string Description { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public byte? PaymentMethodId { get; set; }
    public string? ReceiptUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ExpenseCategory Category { get; set; } = null!;

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual Phase? Phase { get; set; }

    public virtual Project Project { get; set; } = null!;
}
