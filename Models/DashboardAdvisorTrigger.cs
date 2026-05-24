namespace BuildWise.Models;

public class DashboardAdvisorTrigger
{
    public string Title { get; set; } = "";
    public string Severity { get; set; } = "Info";
    public string Message { get; set; } = "";
    public string Category { get; set; } = "General";
    public string ActionUrl { get; set; } = "/Advisor";

    public DashboardAdvisorTrigger()
    {
    }

    public DashboardAdvisorTrigger(string title, string severity, string message, string category, string actionUrl)
    {
        Title = title;
        Severity = severity;
        Message = message;
        Category = category;
        ActionUrl = actionUrl;
    }
}
