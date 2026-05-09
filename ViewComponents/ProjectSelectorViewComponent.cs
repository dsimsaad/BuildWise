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
            .OrderBy(p => p.ProjectName)
            .ToListAsync();

        var selectedProjectId = HttpContext.Session.GetInt32("SelectedProjectId");
        ViewBag.SelectedProjectId = selectedProjectId;
        ViewBag.ActiveProjectTag = selectedProjectId.HasValue ? "Standard" : "Default";

        return View(projects);
    }
}
