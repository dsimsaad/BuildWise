using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class ExpenseCategory
{
    public byte CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
