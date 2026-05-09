using Microsoft.AspNetCore.Mvc;

namespace BuildWise.Controllers;

public class ProjectContextController : Controller
{
    [HttpPost]
    public IActionResult SetActiveProject(int projectId)
    {
        // Store in session
        HttpContext.Session.SetInt32("SelectedProjectId", projectId);
        
        // Return to the previous page or dashboard
        var returnUrl = Request.Headers["Referer"].ToString();
        if (string.IsNullOrEmpty(returnUrl))
        {
            return RedirectToAction("Dashboard", "Home");
        }
        return Redirect(returnUrl);
    }

    [HttpPost]
    public IActionResult ClearProject()
    {
        HttpContext.Session.Remove("SelectedProjectId");
        return RedirectToAction("Dashboard", "Home");
    }
}
