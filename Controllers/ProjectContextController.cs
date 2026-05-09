using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuildWise.Models;
using Microsoft.AspNetCore.Authorization;

namespace BuildWise.Controllers;

[Authorize]
public class ProjectContextController : Controller
{
    private readonly BuildWiseDbContext _context;

    public ProjectContextController(BuildWiseDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> SetActiveProject(int projectId)
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
        if (userIdClaim == null) return Unauthorized();
        int userId = int.Parse(userIdClaim.Value);

        // Verify ownership before setting the session
        bool ownsProject = await _context.Projects.AnyAsync(p => p.ProjectId == projectId && p.UserId == userId);
        
        if (ownsProject)
        {
            HttpContext.Session.SetInt32("SelectedProjectId", projectId);
        }
        else
        {
            // If they don't own it, ensure we don't accidentally leave an old project selected
            HttpContext.Session.Remove("SelectedProjectId");
        }
        
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
        
        var returnUrl = Request.Headers["Referer"].ToString();
        if (string.IsNullOrEmpty(returnUrl))
        {
            return RedirectToAction("Dashboard", "Home");
        }
        return Redirect(returnUrl);
    }
}
