using Microsoft.AspNetCore.Mvc;
using BuildWise.BusinessLayer;

namespace BuildWise.Controllers
{
    public class AdvisorController : Controller
    {
        private readonly AdvisorBLL _bll;

        public AdvisorController(IConfiguration configuration)
        {
            string conn = configuration.GetConnectionString("BuildWise") ?? "";
            _bll = new AdvisorBLL(conn);
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetAnalysis()
        {
            var results = _bll.GetAnalysis();
            return Json(results);
        }
    }
}
