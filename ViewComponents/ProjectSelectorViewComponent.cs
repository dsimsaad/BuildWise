using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BuildWise.Models;
using BuildWise.Services;
using Microsoft.Extensions.Caching.Memory;

namespace BuildWise.ViewComponents;

public class ProjectSelectorViewComponent : ViewComponent
{
    private readonly BuildWiseDbContext _context;
    private readonly IMemoryCache _cache;

    public ProjectSelectorViewComponent(BuildWiseDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userIdClaim = ((ClaimsPrincipal)User).Claims.FirstOrDefault(c => c.Type == "UserId");
        int userId = userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;

        var projects = await _cache.GetOrCreateAsync(ProjectCacheKeys.SelectorProjects(userId), async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(30);
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2);

            return await _context.Projects
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.ProjectName == "main" ? 0 : 1)
                .ThenBy(p => p.ProjectName)
                .Select(p => new Project
                {
                    ProjectId = p.ProjectId,
                    ProjectName = p.ProjectName,
                    IsCompleted = p.IsCompleted
                })
                .ToListAsync();
        }) ?? new List<Project>();

        bool isOverallMode = string.Equals(HttpContext.Request.Query["overall"], "true", StringComparison.OrdinalIgnoreCase);
        var selectedProjectId = isOverallMode ? null : HttpContext.Session.GetInt32("SelectedProjectId");
        if (!isOverallMode && !selectedProjectId.HasValue && projects.Count > 0)
        {
            selectedProjectId = projects[0].ProjectId;
            HttpContext.Session.SetInt32("SelectedProjectId", selectedProjectId.Value);
        }

        var activeProject = selectedProjectId.HasValue
            ? projects.FirstOrDefault(p => p.ProjectId == selectedProjectId.Value)
            : null;

        ViewBag.SelectedProjectId = selectedProjectId;
        ViewBag.IsOverallMode = isOverallMode;
        ViewBag.ActiveProjectTag = isOverallMode
            ? "All Projects"
            : activeProject == null
            ? "No Project"
            : activeProject.IsCompleted ? "Completed" : "Active";

        return View(projects);
    }
}
