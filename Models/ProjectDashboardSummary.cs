namespace BuildWise.Models;

public sealed record ProjectDashboardSummary(
    int ProjectId,
    string ProjectName,
    string PropertyName,
    decimal TotalBudget,
    decimal TotalExpenses,
    decimal RemainingBudget,
    decimal ProgressPercent,
    int TotalTasks,
    int CompletedTasks,
    bool IsCompleted);
