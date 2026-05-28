using Microsoft.AspNetCore.Mvc;
using BuildWise.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using BuildWise.Services;

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
            var rangeLabel = BuildRangeLabel(range, fromDate, toDate);
            var pdf = new TransactionReportPdfBuilder(transactions, rangeLabel).Build();
            var fileName = $"transaction-report-{DateTime.Now:yyyyMMdd-HHmm}.pdf";
            return File(pdf, "application/pdf", fileName);
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

        private static string BuildRangeLabel(string? range, DateTime? fromDate, DateTime? toDate)
        {
            if (fromDate.HasValue || toDate.HasValue)
            {
                var from = fromDate?.ToString("MMM dd, yyyy") ?? "Start";
                var to = toDate?.ToString("MMM dd, yyyy") ?? "Today";
                return $"{from} to {to}";
            }

            return string.Equals(range, "all", StringComparison.OrdinalIgnoreCase)
                ? "All time"
                : "Filtered ledger report";
        }
    }
}
