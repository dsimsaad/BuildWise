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
        private readonly BuildWiseDbContext _context;

        public ConstructionController(IConfiguration configuration, BuildWiseDbContext context)
        {
            string conn = configuration.GetConnectionString("BuildWise") ?? "";
            _bll = new ConstructionBLL(conn);
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        private int? GetSelectedProjectId()
        {
            return HttpContext.Session.GetInt32("SelectedProjectId");
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        private bool UserOwnsProject(int projectId, int userId)
        {
            return _context.Projects.Any(p => p.ProjectId == projectId && p.UserId == userId);
        }

        [HttpGet]
        public IActionResult GetProgressData()
        {
            int userId = GetCurrentUserId();
            var selectedProjectId = GetSelectedProjectId();
            if (selectedProjectId.HasValue && !UserOwnsProject(selectedProjectId.Value, userId))
            {
                HttpContext.Session.Remove("SelectedProjectId");
                selectedProjectId = null;
            }

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
            int userId = GetCurrentUserId();
            var projectId = GetSelectedProjectId();
            if (projectId == null)
                return Json(new { success = false, message = "Please select a project first." });
            if (!UserOwnsProject(projectId.Value, userId))
                return Forbid();

            phase.ProjectId = projectId;
            if (_bll.AddPhase(phase)) return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult UpdatePhase([FromBody] ConstructionPhase phase)
        {
            int userId = GetCurrentUserId();
            if (!_bll.PhaseBelongsToUser(phase.PhaseId, userId))
                return Forbid();

            if (_bll.UpdatePhase(phase)) return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult DeletePhase(int id)
        {
            int userId = GetCurrentUserId();
            if (!_bll.PhaseBelongsToUser(id, userId))
                return Forbid();

            if (_bll.DeletePhase(id)) return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult AddTask([FromBody] PhaseTask task)
        {
            int userId = GetCurrentUserId();
            if (!_bll.PhaseBelongsToUser(task.PhaseId, userId))
                return Forbid();

            if (_bll.AddTask(task)) return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult UpdateTask([FromBody] PhaseTask task)
        {
            int userId = GetCurrentUserId();
            if (!_bll.TaskBelongsToUser(task.TaskId, userId))
                return Forbid();

            if (_bll.UpdateTask(task)) return Json(new { success = true });
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult DeleteTask(int id)
        {
            int userId = GetCurrentUserId();
            if (!_bll.TaskBelongsToUser(id, userId))
                return Forbid();

            if (_bll.DeleteTask(id)) return Json(new { success = true });
            return Json(new { success = false });
        }
    }
}
