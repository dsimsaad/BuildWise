using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BuildWise.Models;
using BuildWise.BusinessLayer;

namespace BuildWise.Controllers
{
    [Authorize]
    public class PropertiesController : Controller
    {
        private readonly PropertyBLL _propertyBll;
        private readonly BuildWiseDbContext _context; // For dropdowns

        public PropertiesController(PropertyBLL propertyBll, BuildWiseDbContext context)
        {
            _propertyBll = propertyBll;
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue("UserId") ?? "0");
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var properties = await _propertyBll.GetUserPropertiesAsync(userId);
            return View(properties);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            var property = await _propertyBll.GetPropertyDetailsAsync(id, userId);
            if (property == null) return NotFound();

            return View(property);
        }

        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PropertyName,TypeId,StatusId,Location,City,AreaSize,AreaUnitId,Notes")] Property property)
        {
            property.UserId = GetCurrentUserId();
            
            // Remove navigation properties from model state validation
            ModelState.Remove("User");
            ModelState.Remove("Type");
            ModelState.Remove("Status");
            ModelState.Remove("AreaUnit");
            ModelState.Remove("Projects");

            if (ModelState.IsValid)
            {
                try
                {
                    await _propertyBll.AddPropertyAsync(property);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            PopulateDropdowns(property);
            return View(property);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            var property = await _propertyBll.GetPropertyDetailsAsync(id, userId);
            if (property == null) return NotFound();

            PopulateDropdowns(property);
            return View(property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PropertyId,PropertyName,TypeId,StatusId,Location,City,AreaSize,AreaUnitId,Notes")] Property property)
        {
            if (id != property.PropertyId) return NotFound();

            var userId = GetCurrentUserId();

            ModelState.Remove("User");
            ModelState.Remove("Type");
            ModelState.Remove("Status");
            ModelState.Remove("AreaUnit");
            ModelState.Remove("Projects");

            if (ModelState.IsValid)
            {
                try
                {
                    await _propertyBll.UpdatePropertyAsync(property, userId);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            PopulateDropdowns(property);
            return View(property);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            await _propertyBll.DeletePropertyAsync(id, userId);
            return RedirectToAction(nameof(Index));
        }

        private void PopulateDropdowns(Property? property = null)
        {
            ViewData["TypeId"] = new SelectList(_context.Set<PropertyType>(), "TypeId", "TypeName", property?.TypeId);
            ViewData["StatusId"] = new SelectList(_context.Set<PropertyStatus>(), "StatusId", "StatusName", property?.StatusId);
            ViewData["AreaUnitId"] = new SelectList(_context.Set<AreaUnit>(), "UnitId", "UnitName", property?.AreaUnitId);
        }
    }
}
