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

        [HttpGet]
        public IActionResult GetTransactions(string category, string type, DateTime? fromDate, DateTime? toDate)
        {
            var list = _bll.GetFilteredTransactions(category, type, fromDate, toDate);
            var totalCount = _bll.GetTotalTransactionsCount();
            var totalAmount = _bll.GetTotalTransactionAmount();

            return Json(new { 
                transactions = list,
                totalCount,
                totalAmount
            });
        }
    }
}
