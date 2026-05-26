using Microsoft.AspNetCore.Mvc;
using BuildWise.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace BuildWise.Controllers
{
    [Authorize]
    public class TransactionController : BaseController
    {
        private readonly TransactionBLL _bll;

        public TransactionController(IConfiguration configuration)
        {
            string conn = configuration.GetConnectionString("BuildWise") ?? "";
            _bll = new TransactionBLL(conn);
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetTransactions(string category, string type, DateTime? fromDate, DateTime? toDate, string range = "month")
        {
            int userId = GetCurrentUserId();
            var selectedProjectId = GetSelectedProjectId();
            ApplyDefaultRange(range, ref fromDate, ref toDate);
            var list = _bll.GetFilteredTransactions(category, type, fromDate, toDate, selectedProjectId, userId);
            var totalCount = list.Count;
            var totalAmount = list.Sum(t => t.Amount);

            return Json(new { 
                transactions = list,
                totalCount,
                totalAmount
            });
        }

        [HttpGet]
        public IActionResult DownloadReport(string category, string type, DateTime? fromDate, DateTime? toDate, string range = "month")
        {
            int userId = GetCurrentUserId();
            var selectedProjectId = GetSelectedProjectId();
            ApplyDefaultRange(range, ref fromDate, ref toDate);
            var transactions = _bll.GetFilteredTransactions(category, type, fromDate, toDate, selectedProjectId, userId);

            var csv = new StringBuilder();
            csv.AppendLine("Date,Type,Category,Amount,Budget Effect,Description");
            foreach (var transaction in transactions)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    EscapeCsv(transaction.TransactionDate.ToString("yyyy-MM-dd HH:mm")),
                    EscapeCsv(transaction.TransactionType),
                    EscapeCsv(transaction.Category),
                    transaction.Amount.ToString("0.##"),
                    transaction.BudgetEffect.ToString("0.##"),
                    EscapeCsv(transaction.Description)
                }));
            }

            var fileName = $"transaction-report-{DateTime.Now:yyyyMMdd-HHmm}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        private static void ApplyDefaultRange(string? range, ref DateTime? fromDate, ref DateTime? toDate)
        {
            if (fromDate.HasValue || toDate.HasValue)
                return;

            var today = DateTime.Today;
            switch ((range ?? "month").Trim().ToLowerInvariant())
            {
                case "year":
                    fromDate = new DateTime(today.Year, 1, 1);
                    toDate = new DateTime(today.Year, 12, 31);
                    break;
                case "all":
                    break;
                default:
                    fromDate = new DateTime(today.Year, today.Month, 1);
                    toDate = fromDate.Value.AddMonths(1).AddDays(-1);
                    break;
            }
        }

        private static string EscapeCsv(string? value)
        {
            var text = value ?? "";
            if (!text.Contains(',') && !text.Contains('"') && !text.Contains('\n') && !text.Contains('\r'))
                return text;

            return $"\"{text.Replace("\"", "\"\"")}\"";
        }
    }
}
