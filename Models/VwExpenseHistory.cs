using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class VwExpenseHistory
{
    public int ExpenseId { get; set; }

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = null!;

    public string CategoryName { get; set; } = null!;

    public int? PhaseId { get; set; }

    public string PhaseName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateOnly ExpenseDate { get; set; }

    public string? PaymentMethod { get; set; }

    public string? ReceiptUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}
