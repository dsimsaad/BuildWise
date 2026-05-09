using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        var projects = await _context.Projects
            .OrderBy(p => p.ProjectName)
            .ToListAsync();

        var selectedProjectId = HttpContext.Session.GetInt32("SelectedProjectId");
        ViewBag.SelectedProjectId = selectedProjectId;

        return View(projects);
    }
}
