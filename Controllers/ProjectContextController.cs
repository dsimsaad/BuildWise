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
        return Redirect(RemoveOverallQuery(returnUrl));
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
        return Redirect(RemoveOverallQuery(returnUrl));
    }

    private static string RemoveOverallQuery(string returnUrl)
    {
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
            return returnUrl.Replace("?overall=true", "", StringComparison.OrdinalIgnoreCase)
                .Replace("&overall=true", "", StringComparison.OrdinalIgnoreCase);

        var queryParts = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query)
            .Where(kvp => !string.Equals(kvp.Key, "overall", StringComparison.OrdinalIgnoreCase))
            .SelectMany(kvp => kvp.Value.Select(value =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(value ?? "")}"));

        var builder = new UriBuilder(uri)
        {
            Query = string.Join("&", queryParts)
        };

        return builder.Uri.PathAndQuery;
    }
}
