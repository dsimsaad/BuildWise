using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class Expense
{
    /// <summary>
    /// Auto-increment PK
    /// </summary>
    public int ExpenseId { get; set; }

    public int ProjectId { get; set; }

    public int? PhaseId { get; set; }

    /// <summary>
    /// FK to ExpenseCategory: Labor/Material/Equipment/Transport/Misc
    /// </summary>
    public byte CategoryId { get; set; }

    public string Description { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public byte? PaymentMethodId { get; set; }

    /// <summary>
    /// Path/URL to uploaded receipt image for proof
    /// </summary>
    public string? ReceiptUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ExpenseCategory Category { get; set; } = null!;

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual Phase? Phase { get; set; }

    public virtual Project Project { get; set; } = null!;
}
