using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BuildWise.Models;
using BuildWise.BusinessLayer;

namespace BuildWise.Controllers
{
    [Authorize]
    public class MaterialsController : BaseController
    {
        private readonly MaterialBLL _materialBll;
        private readonly ExpenseBLL _expenseBll;
        private readonly BuildWiseDbContext _context;

        public MaterialsController(MaterialBLL materialBll, BuildWiseDbContext context, IConfiguration configuration)
        {
            _materialBll = materialBll;
            _context = context;
            var connectionString = configuration.GetConnectionString("BuildWise") ?? "";
            _expenseBll = new ExpenseBLL(connectionString);
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var projectId = GetSelectedProjectId();
            if (projectId == null)
            {
                TempData["WarningMessage"] = "Please select a specific project from the top navigation to view material purchases.";
                return RedirectToAction("Index", "Projects");
            }

            if (!await UserOwnsProjectAsync(projectId.Value, userId))
            {
                HttpContext.Session.Remove("SelectedProjectId");
                return RedirectToAction("Index", "Projects");
            }

            var purchases = await _materialBll.GetProjectPurchasesAsync(projectId.Value);
            ViewBag.SelectedProjectId = projectId.Value;
            ViewBag.ProjectPhases = await GetProjectPhaseOptionsAsync(projectId.Value);
            return View(purchases);
        }

        public IActionResult CreatePurchase()
        {
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");

            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePurchase([Bind("MaterialId,Quantity,UnitId,UnitPrice,PurchaseDate,Notes")] MaterialPurchase purchase)
        {
            var userId = GetCurrentUserId();
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");

            if (!await UserOwnsProjectAsync(projectId.Value, userId))
            {
                HttpContext.Session.Remove("SelectedProjectId");
                return RedirectToAction("Index", "Projects");
            }

            RemoveModelStateEntries("Material", "Unit", "Project", "Supplier", "MaterialUsages");

            bool materialAllowed = await _context.Materials
                .AsNoTracking()
                .AnyAsync(m => m.MaterialId == purchase.MaterialId && m.IsActive && (m.UserId == userId || m.UserId == 1));
            if (!materialAllowed)
            {
                ModelState.AddModelError(nameof(MaterialPurchase.MaterialId), "Please select a valid material.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    purchase.SupplierId = null;
                    purchase.InvoiceNumber = null;
                    await _materialBll.AddPurchaseAsync(purchase, projectId.Value);
                    AddMaterialExpense(purchase);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            PopulateDropdowns(purchase);
            return View(purchase);
        }

        public async Task<IActionResult> EditPurchase(int id)
        {
            var userId = GetCurrentUserId();
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");
            if (!await UserOwnsProjectAsync(projectId.Value, userId))
            {
                HttpContext.Session.Remove("SelectedProjectId");
                return RedirectToAction("Index", "Projects");
            }

            var purchase = await _context.MaterialPurchases
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PurchaseId == id && p.ProjectId == projectId.Value);
            if (purchase == null) return NotFound();

            PopulateDropdowns(purchase);
            return View("CreatePurchase", purchase);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPurchase(int id, [Bind("PurchaseId,MaterialId,Quantity,UnitId,UnitPrice,PurchaseDate,Notes")] MaterialPurchase purchase)
        {
            if (id != purchase.PurchaseId) return NotFound();

            var userId = GetCurrentUserId();
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");
            if (!await UserOwnsProjectAsync(projectId.Value, userId))
            {
                HttpContext.Session.Remove("SelectedProjectId");
                return RedirectToAction("Index", "Projects");
            }

            RemoveModelStateEntries("Material", "Unit", "Project", "Supplier", "MaterialUsages");

            bool materialAllowed = await _context.Materials
                .AsNoTracking()
                .AnyAsync(m => m.MaterialId == purchase.MaterialId && m.IsActive && (m.UserId == userId || m.UserId == 1));
            if (!materialAllowed)
            {
                ModelState.AddModelError(nameof(MaterialPurchase.MaterialId), "Please select a valid material.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingPurchase = await _context.MaterialPurchases
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.PurchaseId == id && p.ProjectId == projectId.Value);
                    if (existingPurchase == null) return NotFound();

                    var updatedPurchase = await _materialBll.UpdatePurchaseAsync(purchase, projectId.Value);
                    DeleteMaterialExpense(existingPurchase);
                    AddMaterialExpense(updatedPurchase);
                    TempData["MaterialMessage"] = "Purchase updated and material expense synced.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            PopulateDropdowns(purchase);
            return View("CreatePurchase", purchase);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePurchase(int id)
        {
            var userId = GetCurrentUserId();
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");

            if (!await UserOwnsProjectAsync(projectId.Value, userId))
            {
                HttpContext.Session.Remove("SelectedProjectId");
                return RedirectToAction("Index", "Projects");
            }

            var purchase = await _context.MaterialPurchases
                .AsNoTracking()
                .Include(p => p.Material)
                .FirstOrDefaultAsync(p => p.PurchaseId == id && p.ProjectId == projectId.Value);
            if (purchase != null)
            {
                DeleteMaterialExpense(purchase);
            }
            await _materialBll.DeletePurchaseAsync(id, projectId.Value);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnPurchase(int id, decimal returnQuantity, string? returnNotes)
        {
            var userId = GetCurrentUserId();
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");

            if (!await UserOwnsProjectAsync(projectId.Value, userId))
            {
                HttpContext.Session.Remove("SelectedProjectId");
                return RedirectToAction("Index", "Projects");
            }

            try
            {
                var purchase = await _materialBll.ReturnPurchaseAsync(id, projectId.Value, returnQuantity);
                var returnAmount = Math.Round(returnQuantity * purchase.UnitPrice, 2);

                if (!_expenseBll.ApplyMaterialReturn(purchase, returnQuantity, returnAmount, returnNotes))
                {
                    TempData["MaterialMessage"] = "Return saved, but the expense adjustment could not be fully logged.";
                }
                else
                {
                    TempData["MaterialMessage"] = $"Returned {returnQuantity:0.###} {purchase.Unit?.UnitName} of {purchase.Material?.MaterialName}. Expense reduced by PKR {returnAmount:N0}.";
                }
            }
            catch (Exception ex)
            {
                TempData["MaterialError"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordUsage(int id, int phaseId, decimal quantityUsed, DateOnly usageDate, string? usageNotes)
        {
            var userId = GetCurrentUserId();
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");

            if (!await UserOwnsProjectAsync(projectId.Value, userId))
            {
                HttpContext.Session.Remove("SelectedProjectId");
                return RedirectToAction("Index", "Projects");
            }

            var phaseAllowed = await _context.Phases
                .AnyAsync(p => p.PhaseId == phaseId && p.ProjectId == projectId.Value && p.Project.UserId == userId);
            if (!phaseAllowed)
            {
                TempData["MaterialError"] = "Select a valid phase before recording material usage.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var purchase = await _materialBll.RecordUsageAsync(id, projectId.Value, phaseId, quantityUsed, usageDate, usageNotes);
                TempData["MaterialMessage"] = $"Recorded {quantityUsed:0.###} {purchase.Unit?.UnitName} of {purchase.Material?.MaterialName} as used.";
            }
            catch (Exception ex)
            {
                TempData["MaterialError"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Catalog()
        {
            var userId = GetCurrentUserId();
            var materials = await _materialBll.GetUserMaterialsAsync(userId);
            ViewBag.CurrentUserId = userId;
            return View(materials);
        }

        public IActionResult AddMaterial()
        {
            ViewData["DefaultUnitId"] = new SelectList(_context.Set<MaterialUnit>(), "UnitId", "UnitName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMaterial([Bind("MaterialName,DefaultUnitId,Description")] Material material)
        {
            RemoveModelStateEntries("DefaultUnit", "User", "MaterialPurchases");

            if (ModelState.IsValid)
            {
                var userId = GetCurrentUserId();
                await _materialBll.AddMaterialAsync(material, userId);
                return RedirectToAction(nameof(Catalog));
            }
            ViewData["DefaultUnitId"] = new SelectList(_context.Set<MaterialUnit>(), "UnitId", "UnitName", material.DefaultUnitId);
            return View(material);
        }

        public async Task<IActionResult> EditMaterial(int id)
        {
            try
            {
                var material = await _materialBll.GetEditableMaterialAsync(id, GetCurrentUserId());
                ViewData["DefaultUnitId"] = new SelectList(_context.Set<MaterialUnit>(), "UnitId", "UnitName", material.DefaultUnitId);
                return View("AddMaterial", material);
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMaterial(int id, [Bind("MaterialId,MaterialName,DefaultUnitId,Description")] Material material)
        {
            if (id != material.MaterialId) return NotFound();
            RemoveModelStateEntries("DefaultUnit", "User", "MaterialPurchases");

            if (ModelState.IsValid)
            {
                try
                {
                    await _materialBll.UpdateMaterialAsync(material, GetCurrentUserId());
                    TempData["MaterialMessage"] = "Material updated.";
                    return RedirectToAction(nameof(Catalog));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            ViewData["DefaultUnitId"] = new SelectList(_context.Set<MaterialUnit>(), "UnitId", "UnitName", material.DefaultUnitId);
            return View("AddMaterial", material);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMaterialActive(int id, bool isActive)
        {
            try
            {
                await _materialBll.SetMaterialActiveAsync(id, GetCurrentUserId(), isActive);
                TempData["MaterialMessage"] = isActive ? "Material activated." : "Material deactivated.";
            }
            catch (Exception ex)
            {
                TempData["MaterialError"] = ex.Message;
            }

            return RedirectToAction(nameof(Catalog));
        }

        private void PopulateDropdowns(MaterialPurchase? purchase = null)
        {
            var userId = GetCurrentUserId();
            var materials = _context.Materials
                .Where(m => (m.UserId == userId || m.UserId == 1) && m.IsActive)
                .OrderBy(m => m.MaterialName)
                .ToList();
            ViewData["MaterialId"] = new SelectList(materials, "MaterialId", "MaterialName", purchase?.MaterialId);
            ViewData["UnitId"] = new SelectList(_context.Set<MaterialUnit>(), "UnitId", "UnitName", purchase?.UnitId);
        }

        private async Task<bool> UserOwnsProjectAsync(int projectId, int userId)
        {
            return await _context.Projects.AnyAsync(p => p.ProjectId == projectId && p.UserId == userId);
        }

        private void AddMaterialExpense(MaterialPurchase purchase)
        {
            _expenseBll.AddExpense(new ExpenseItem
            {
                ProjectId = purchase.ProjectId,
                Category = "Material",
                Description = BuildMaterialExpenseDescription(purchase),
                Amount = purchase.TotalCost ?? purchase.Quantity * purchase.UnitPrice,
                ExpenseDate = purchase.PurchaseDate.ToDateTime(TimeOnly.MinValue)
            });
        }

        private void DeleteMaterialExpense(MaterialPurchase purchase)
        {
            var description = BuildMaterialExpenseDescription(purchase);
            var amount = purchase.TotalCost ?? purchase.Quantity * purchase.UnitPrice;
            var expenseDate = purchase.PurchaseDate.ToDateTime(TimeOnly.MinValue).Date;
            // Older records may still include the material name after the purchase number.
            var matchingExpense = _expenseBll
                .GetAllExpenses(purchase.ProjectId)
                .FirstOrDefault(e =>
                    e.ProjectId == purchase.ProjectId &&
                    string.Equals(e.Category, "Material", StringComparison.OrdinalIgnoreCase) &&
                    (string.Equals(e.Description, description, StringComparison.OrdinalIgnoreCase) ||
                     e.Description.StartsWith(description + ":", StringComparison.OrdinalIgnoreCase)) &&
                    e.ExpenseDate.Date == expenseDate &&
                    e.Amount == amount);

            if (matchingExpense != null)
            {
                _expenseBll.DeleteExpense(matchingExpense.ExpenseId);
            }
        }

        private static string BuildMaterialExpenseDescription(MaterialPurchase purchase)
        {
            return $"Material purchase #{purchase.PurchaseId}";
        }

        private async Task<List<SelectListItem>> GetProjectPhaseOptionsAsync(int projectId)
        {
            return await _context.Phases
                .AsNoTracking()
                .Include(p => p.PhaseType)
                .Include(p => p.Property)
                .Where(p => p.ProjectId == projectId)
                .OrderBy(p => p.Property != null ? p.Property.PropertyName : "")
                .ThenBy(p => p.Sequence)
                .Select(p => new SelectListItem
                {
                    Value = p.PhaseId.ToString(),
                    Text = (p.Property != null ? p.Property.PropertyName + " - " : "") +
                           (string.IsNullOrWhiteSpace(p.CustomPhaseName) ? p.PhaseType.PhaseName : p.CustomPhaseName)
                })
                .ToListAsync();
        }
    }
}
