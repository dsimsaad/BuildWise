namespace BuildWise.Models
{

    public class AdvisorResult
    {
        public string RuleName { get; set; }
        public string Severity { get; set; }   
        public string Message { get; set; }
        public string Category { get; set; }   

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
