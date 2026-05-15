using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BuildWise.Models;

namespace BuildWise.ViewComponents;

public class ProjectSelectorViewComponent : ViewComponent
{
    private readonly BuildWiseDbContext _context;

    public ProjectSelectorViewComponent(BuildWiseDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userIdClaim = ((ClaimsPrincipal)User).Claims.FirstOrDefault(c => c.Type == "UserId");
        int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

        var projects = await _context.Projects
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.ProjectName == "main" ? 0 : 1)
            .ThenBy(p => p.ProjectName)
            .ToListAsync();

        var selectedProjectId = HttpContext.Session.GetInt32("SelectedProjectId");
        if (!selectedProjectId.HasValue && projects.Count > 0)
        {
            selectedProjectId = projects[0].ProjectId;
            HttpContext.Session.SetInt32("SelectedProjectId", selectedProjectId.Value);
        }

        var activeProject = selectedProjectId.HasValue
            ? projects.FirstOrDefault(p => p.ProjectId == selectedProjectId.Value)
            : null;

        ViewBag.SelectedProjectId = selectedProjectId;
        ViewBag.ActiveProjectTag = activeProject == null
            ? "No Project"
            : activeProject.IsCompleted ? "Completed" : "Active";

        return View(projects);
    }
}
