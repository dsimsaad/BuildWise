using System;
using System.Security.Claims;
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
    public class MaterialsController : Controller
    {
        private readonly MaterialBLL _materialBll;
        private readonly TransactionBLL _transactionBll;
        private readonly BuildWiseDbContext _context;

        public MaterialsController(MaterialBLL materialBll, BuildWiseDbContext context, IConfiguration configuration)
        {
            _materialBll = materialBll;
            _context = context;
            _transactionBll = new TransactionBLL(configuration.GetConnectionString("BuildWise") ?? "");
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue("UserId") ?? "0");
        }

        private int? GetSelectedProjectId()
        {
            return HttpContext.Session.GetInt32("SelectedProjectId");
        }

        // Dashboard/Index for Purchases
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
            return View(purchases);
        }

        // GET: /Materials/CreatePurchase
        public IActionResult CreatePurchase()
        {
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");

            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePurchase([Bind("MaterialId,SupplierId,Quantity,UnitId,UnitPrice,PurchaseDate,InvoiceNumber,Notes")] MaterialPurchase purchase)
        {
            var userId = GetCurrentUserId();
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");

            if (!await UserOwnsProjectAsync(projectId.Value, userId))
            {
                HttpContext.Session.Remove("SelectedProjectId");
                return RedirectToAction("Index", "Projects");
            }

            ModelState.Remove("Material");
            ModelState.Remove("Unit");
            ModelState.Remove("Project");
            ModelState.Remove("Supplier");
            ModelState.Remove("MaterialUsages");

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
                    await _materialBll.AddPurchaseAsync(purchase, projectId.Value);
                    LogMaterialTransaction("Added", purchase, "Material purchase logged.");
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
            await _materialBll.DeletePurchaseAsync(id, projectId.Value);
            if (purchase != null)
            {
                LogMaterialTransaction("Deleted", purchase, "Material purchase deleted.");
            }
            return RedirectToAction(nameof(Index));
        }

        // Material Usage
        public async Task<IActionResult> LogUsage(int purchaseId)
        {
            var userId = GetCurrentUserId();
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");

            if (!await UserOwnsProjectAsync(projectId.Value, userId))
            {
                HttpContext.Session.Remove("SelectedProjectId");
                return RedirectToAction("Index", "Projects");
            }

            var purchase = await _materialBll.GetProjectPurchasesAsync(projectId.Value);
            var selectedPurchase = purchase.FirstOrDefault(p => p.PurchaseId == purchaseId);
            
            if (selectedPurchase == null) return NotFound();

            var usage = new MaterialUsage { PurchaseId = purchaseId };
            
            // Populate phases for the dropdown
            PopulatePhaseDropdown(projectId.Value);
            
            var used = selectedPurchase.MaterialUsages.Sum(u => u.QuantityUsed);
            ViewBag.PurchaseDetails = $"{selectedPurchase.Material.MaterialName} ({selectedPurchase.Quantity - used} {selectedPurchase.Unit.UnitName} remaining)";
            return View(usage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogUsage([Bind("PurchaseId,PhaseId,QuantityUsed,UsageDate,Notes")] MaterialUsage usage)
        {
            var userId = GetCurrentUserId();
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");

            if (!await UserOwnsProjectAsync(projectId.Value, userId))
            {
                HttpContext.Session.Remove("SelectedProjectId");
                return RedirectToAction("Index", "Projects");
            }

            ModelState.Remove("Phase");
            ModelState.Remove("Purchase");

            var purchase = await _context.MaterialPurchases
                .Include(p => p.MaterialUsages)
                .FirstOrDefaultAsync(p => p.PurchaseId == usage.PurchaseId && p.ProjectId == projectId.Value);
            if (purchase == null)
            {
                return NotFound();
            }

            bool phaseBelongsToProject = await _context.Phases
                .AnyAsync(p => p.PhaseId == usage.PhaseId && p.ProjectId == projectId.Value);
            if (!phaseBelongsToProject)
            {
                ModelState.AddModelError(nameof(MaterialUsage.PhaseId), "Please select a valid project phase.");
            }

            var alreadyUsed = purchase.MaterialUsages.Sum(u => u.QuantityUsed);
            var remaining = purchase.Quantity - alreadyUsed;
            if (usage.QuantityUsed <= 0)
            {
                ModelState.AddModelError(nameof(MaterialUsage.QuantityUsed), "Quantity used must be greater than zero.");
            }
            else if (usage.QuantityUsed > remaining)
            {
                ModelState.AddModelError(nameof(MaterialUsage.QuantityUsed), $"Only {remaining} is remaining from this purchase.");
            }

            if (ModelState.IsValid)
            {
                _context.MaterialUsages.Add(usage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulatePhaseDropdown(projectId.Value, usage.PhaseId);
            var used = purchase.MaterialUsages.Sum(u => u.QuantityUsed);
            ViewBag.PurchaseDetails = $"Only {purchase.Quantity - used} remaining from this purchase";
            return View(usage);
        }

        // Catalog Management
        public async Task<IActionResult> Catalog()
        {
            var userId = GetCurrentUserId();
            var materials = await _materialBll.GetUserMaterialsAsync(userId);
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
            ModelState.Remove("DefaultUnit");
            ModelState.Remove("User");
            ModelState.Remove("MaterialPurchases");

            if (ModelState.IsValid)
            {
                var userId = GetCurrentUserId();
                await _materialBll.AddMaterialAsync(material, userId);
                return RedirectToAction(nameof(Catalog));
            }
            ViewData["DefaultUnitId"] = new SelectList(_context.Set<MaterialUnit>(), "UnitId", "UnitName", material.DefaultUnitId);
            return View(material);
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
            ViewData["SupplierId"] = new SelectList(_context.Set<Supplier>(), "SupplierId", "SupplierName", purchase?.SupplierId);
        }

        private void PopulatePhaseDropdown(int projectId, int? selectedPhaseId = null)
        {
            var phases = _context.Phases
                .AsNoTracking()
                .Include(p => p.PhaseType)
                .Where(p => p.ProjectId == projectId)
                .OrderBy(p => p.Sequence)
                .Select(p => new
                {
                    p.PhaseId,
                    PhaseName = string.IsNullOrWhiteSpace(p.CustomPhaseName) ? p.PhaseType.PhaseName : p.CustomPhaseName
                })
                .ToList();

            ViewData["PhaseId"] = new SelectList(phases, "PhaseId", "PhaseName", selectedPhaseId);
        }

        private async Task<bool> UserOwnsProjectAsync(int projectId, int userId)
        {
            return await _context.Projects.AnyAsync(p => p.ProjectId == projectId && p.UserId == userId);
        }

        private void LogMaterialTransaction(string type, MaterialPurchase purchase, string description)
        {
            var materialName = purchase.Material?.MaterialName;
            if (string.IsNullOrWhiteSpace(materialName) && purchase.MaterialId > 0)
            {
                materialName = _context.Materials
                    .AsNoTracking()
                    .Where(m => m.MaterialId == purchase.MaterialId)
                    .Select(m => m.MaterialName)
                    .FirstOrDefault();
            }

            _transactionBll.AddTransaction(new TransactionLog
            {
                ProjectId = purchase.ProjectId,
                TransactionType = type,
                Category = "Material",
                Description = $"{description} {materialName}".Trim(),
                Amount = purchase.TotalCost ?? purchase.Quantity * purchase.UnitPrice,
                BudgetEffect = 0
            });
        }
    }
}
