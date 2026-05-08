using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class PaymentMethod
{
    public byte MethodId { get; set; }

    public string MethodName { get; set; } = null!;

    public virtual ICollection<ClientPayment> ClientPayments { get; set; } = new List<ClientPayment>();

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual ICollection<WagePayment> WagePayments { get; set; } = new List<WagePayment>();
}
