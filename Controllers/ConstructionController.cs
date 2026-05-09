using Microsoft.AspNetCore.Mvc;
using BuildWise.Models;
using BuildWise.BusinessLayer;
using Microsoft.AspNetCore.Authorization;

namespace BuildWise.Controllers
{
    [Authorize]
    public class ConstructionController : Controller
    {
        private readonly ConstructionBLL _bll;

        public ConstructionController(IConfiguration configuration)
        {
            string conn = configuration.GetConnectionString("BuildWise") ?? "";
            _bll = new ConstructionBLL(conn);
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
        public IActionResult GetProgressData()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

            var selectedProjectId = GetSelectedProjectId();
            var structure = _bll.GetFullProjectStructure(selectedProjectId, userId);
            var overallProgress = _bll.CalculateOverallProgress(selectedProjectId, userId);
            
            return Json(new { 
                phases = structure,
                overallProgress
            });
        }

        [HttpPost]
        public IActionResult AddPhase([FromBody] ConstructionPhase phase)
        {
            phase.ProjectId = GetSelectedProjectId();
            if (_bll.AddPhase(phase)) return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult UpdatePhase([FromBody] ConstructionPhase phase)
        {
            if (_bll.UpdatePhase(phase)) return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult DeletePhase(int id)
        {
            if (_bll.DeletePhase(id)) return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult AddTask([FromBody] PhaseTask task)
        {
            if (_bll.AddTask(task)) return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult UpdateTask([FromBody] PhaseTask task)
        {
            if (_bll.UpdateTask(task)) return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult DeleteTask(int id)
        {
            if (_bll.DeleteTask(id)) return Json(new { success = true });
            return Json(new { success = false });
        }
    }
}
