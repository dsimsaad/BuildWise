using Microsoft.AspNetCore.Mvc;
using BuildWise.BusinessLayer;
using Microsoft.AspNetCore.Authorization;

namespace BuildWise.Controllers
{
    [Authorize]
    public class TransactionController : Controller
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

        private int? GetSelectedProjectId()
        {
            return HttpContext.Session.GetInt32("SelectedProjectId");
        }

        [HttpGet]
        public IActionResult GetTransactions(string category, string type, DateTime? fromDate, DateTime? toDate)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

            var selectedProjectId = GetSelectedProjectId();
            var list = _bll.GetFilteredTransactions(category, type, fromDate, toDate, selectedProjectId, userId);
            var totalCount = _bll.GetTotalTransactionsCount(selectedProjectId, userId);
            var totalAmount = _bll.GetTotalTransactionAmount(selectedProjectId, userId);

            return Json(new { 
                transactions = list,
                totalCount,
                totalAmount
            });
        }
    }
}
