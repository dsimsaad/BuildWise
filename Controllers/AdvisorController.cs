using Microsoft.AspNetCore.Mvc;
using BuildWise.BusinessLayer;
using Microsoft.AspNetCore.Authorization;

namespace BuildWise.Controllers
{
    [Authorize]
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

        private int? GetSelectedProjectId()
        {
            return HttpContext.Session.GetInt32("SelectedProjectId");
        }

        [HttpGet]
        public IActionResult GetAnalysis()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

            var results = _bll.GetAnalysis(GetSelectedProjectId(), userId);
            return Json(results);
        }
    }
}
