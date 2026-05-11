using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using BuildWise.Models;
using BuildWise.BusinessLayer;

namespace BuildWise.Controllers
{
    [Authorize]
    public class MaterialsController : Controller
    {
        private readonly MaterialBLL _materialBll;
        private readonly BuildWiseDbContext _context;

        public MaterialsController(MaterialBLL materialBll, BuildWiseDbContext context)
        {
            _materialBll = materialBll;
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }

        private int? GetSelectedProjectId()
        {
            return HttpContext.Session.GetInt32("SelectedProjectId");
        }

        // Dashboard/Index for Purchases
        public async Task<IActionResult> Index()
        {
            var projectId = GetSelectedProjectId();
            if (projectId == null)
            {
                TempData["WarningMessage"] = "Please select a specific project from the top navigation to view material purchases.";
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
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");

            ModelState.Remove("Material");
            ModelState.Remove("Unit");
            ModelState.Remove("Project");
            ModelState.Remove("Supplier");
            ModelState.Remove("MaterialUsages");

            if (ModelState.IsValid)
            {
                try
                {
                    await _materialBll.AddPurchaseAsync(purchase, projectId.Value);
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
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");

            await _materialBll.DeletePurchaseAsync(id, projectId.Value);
            return RedirectToAction(nameof(Index));
        }

        // Material Usage
        public async Task<IActionResult> LogUsage(int purchaseId)
        {
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");

            var purchase = await _materialBll.GetProjectPurchasesAsync(projectId.Value);
            var selectedPurchase = purchase.FirstOrDefault(p => p.PurchaseId == purchaseId);
            
            if (selectedPurchase == null) return NotFound();

            var usage = new MaterialUsage { PurchaseId = purchaseId };
            
            // Populate phases for the dropdown
            var phases = _context.Phases.Where(p => p.ProjectId == projectId.Value).ToList();
            ViewData["PhaseId"] = new SelectList(phases, "PhaseId", "PhaseName");
            
            ViewBag.PurchaseDetails = $"{selectedPurchase.Material.MaterialName} ({selectedPurchase.Quantity} {selectedPurchase.Unit.UnitName} available)";
            return View(usage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogUsage([Bind("PurchaseId,PhaseId,QuantityUsed,UsageDate,Notes")] MaterialUsage usage)
        {
            var projectId = GetSelectedProjectId();
            if (projectId == null) return RedirectToAction("Index", "Projects");

            ModelState.Remove("Phase");
            ModelState.Remove("Purchase");

            if (ModelState.IsValid)
            {
                _context.MaterialUsages.Add(usage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var phases = _context.Phases.Where(p => p.ProjectId == projectId.Value).ToList();
            ViewData["PhaseId"] = new SelectList(phases, "PhaseId", "PhaseName", usage.PhaseId);
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
            // User-specific materials
            var materials = _context.Materials.Where(m => m.UserId == userId && m.IsActive).ToList();
            ViewData["MaterialId"] = new SelectList(materials, "MaterialId", "MaterialName", purchase?.MaterialId);
            
            ViewData["UnitId"] = new SelectList(_context.Set<MaterialUnit>(), "UnitId", "UnitName", purchase?.UnitId);
            ViewData["SupplierId"] = new SelectList(_context.Set<Supplier>(), "SupplierId", "SupplierName", purchase?.SupplierId);
        }
    }
}
