using System;
using System.Collections.Generic;

namespace BuildWise.Models;

public partial class VwContractorSummary
{
    public int ContractorId { get; set; }

    public string ContractorName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public decimal? ContractCost { get; set; }

    public int? TotalTasksAssigned { get; set; }

    public int? CompletedTasks { get; set; }

    public int? InProgressTasks { get; set; }

    public int? PendingTasks { get; set; }

    public int? WorkersUnder { get; set; }
}
