using Microsoft.AspNetCore.Mvc;
using BuildWise.BusinessLayer;
using Microsoft.AspNetCore.Authorization;

namespace BuildWise.Controllers
{
    [Authorize]
    public class AdvisorController : BaseController
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
            int userId = GetCurrentUserId();
            var results = _bll.GetAnalysis(GetSelectedProjectId(), userId);
            return Json(results);
        }
    }
}
