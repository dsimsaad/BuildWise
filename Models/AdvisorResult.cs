namespace BuildWise.Models
{
    /// <summary>
    /// Represents an advisor rule result shown on the Cost Advisor page
    /// </summary>
    public class AdvisorResult
    {
        public string RuleName { get; set; }
        public string Severity { get; set; }   // Warning, Alert, Caution, Info, Success
        public string Message { get; set; }
        public string Category { get; set; }   // which category it relates to (or "General")

        public AdvisorResult()
        {
            RuleName = "";
            Severity = "Info";
            Message = "";
            Category = "General";
        }

        public AdvisorResult(string ruleName, string severity, string message, string category)
        {
            RuleName = ruleName;
            Severity = severity;
            Message = message;
            Category = category;
        }
    }
}
