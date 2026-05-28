using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using BuildWise.Models;
using BuildWise.DataLayer;

namespace BuildWise.BusinessLayer
{
    public class MaterialBLL
    {
        private readonly MaterialDAL _materialDal;

        public MaterialBLL(MaterialDAL materialDal)
        {
            _materialDal = materialDal;
        }

        public async Task<List<Material>> GetUserMaterialsAsync(int userId)
        {
            return await _materialDal.GetMaterialsByUserIdAsync(userId);
        }

        public async Task AddMaterialAsync(Material material, int userId)
        {
            if (string.IsNullOrWhiteSpace(material.MaterialName))
                throw new ArgumentException("Material name is required.");
            
            material.UserId = userId;
            material.IsActive = true;
            await _materialDal.AddMaterialAsync(material);
        }

        public async Task<Material> GetEditableMaterialAsync(int materialId, int userId)
        {
            var material = await _materialDal.GetMaterialByIdAsync(materialId, userId);
            if (material == null)
                throw new ArgumentException("Material was not found.");

            return material;
        }

        public async Task UpdateMaterialAsync(Material updatedMaterial, int userId)
        {
            if (string.IsNullOrWhiteSpace(updatedMaterial.MaterialName))
                throw new ArgumentException("Material name is required.");

            var material = await GetEditableMaterialAsync(updatedMaterial.MaterialId, userId);
            material.MaterialName = updatedMaterial.MaterialName.Trim();
            material.DefaultUnitId = updatedMaterial.DefaultUnitId;
            material.Description = string.IsNullOrWhiteSpace(updatedMaterial.Description) ? null : updatedMaterial.Description.Trim();
            await _materialDal.UpdateMaterialAsync(material);
        }

        public async Task SetMaterialActiveAsync(int materialId, int userId, bool isActive)
        {
            var material = await GetEditableMaterialAsync(materialId, userId);
            material.IsActive = isActive;
            await _materialDal.UpdateMaterialAsync(material);
        }

        public async Task<List<MaterialPurchase>> GetProjectPurchasesAsync(int projectId)
        {
            return await _materialDal.GetPurchasesByProjectIdAsync(projectId);
        }

        public async Task AddPurchaseAsync(MaterialPurchase purchase, int projectId)
        {
            if (purchase.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");
            if (purchase.UnitPrice < 0)
                throw new ArgumentException("Unit price cannot be negative.");
            if (purchase.PurchaseDate == default)
                purchase.PurchaseDate = DateOnly.FromDateTime(DateTime.Today);

            purchase.ProjectId = projectId;
            
            await _materialDal.AddPurchaseAsync(purchase);
        }

        public async Task<MaterialPurchase> UpdatePurchaseAsync(MaterialPurchase updatedPurchase, int projectId)
        {
            if (updatedPurchase.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");
            if (updatedPurchase.UnitPrice < 0)
                throw new ArgumentException("Unit price cannot be negative.");
            if (updatedPurchase.PurchaseDate == default)
                updatedPurchase.PurchaseDate = DateOnly.FromDateTime(DateTime.Today);

            var purchase = await _materialDal.GetPurchaseByIdAsync(updatedPurchase.PurchaseId, projectId);
            if (purchase == null)
                throw new ArgumentException("Purchase record was not found.");

            var usedQuantity = purchase.MaterialUsages.Sum(u => u.QuantityUsed);
            if (updatedPurchase.Quantity < usedQuantity)
                throw new ArgumentException($"Purchase quantity cannot be less than the used quantity ({usedQuantity:0.###}).");

            // Existing usage records stay linked to the purchase, so only editable purchase fields are changed.
            purchase.MaterialId = updatedPurchase.MaterialId;
            purchase.Quantity = updatedPurchase.Quantity;
            purchase.UnitId = updatedPurchase.UnitId;
            purchase.UnitPrice = updatedPurchase.UnitPrice;
            purchase.PurchaseDate = updatedPurchase.PurchaseDate;
            purchase.Notes = string.IsNullOrWhiteSpace(updatedPurchase.Notes) ? null : updatedPurchase.Notes.Trim();

            await _materialDal.UpdatePurchaseAsync(purchase);
            return purchase;
        }

        public async Task<MaterialPurchase> ReturnPurchaseAsync(int purchaseId, int projectId, decimal returnQuantity)
        {
            if (returnQuantity <= 0)
                throw new ArgumentException("Return quantity must be greater than zero.");

            var purchase = await _materialDal.GetPurchaseByIdAsync(purchaseId, projectId);
            if (purchase == null)
                throw new ArgumentException("Purchase record was not found.");

            var usedQuantity = purchase.MaterialUsages.Sum(u => u.QuantityUsed);
            var availableQuantity = purchase.Quantity - usedQuantity;

            if (availableQuantity <= 0)
                throw new ArgumentException("There is no unused quantity available to return.");

            if (returnQuantity > availableQuantity)
                throw new ArgumentException($"Return quantity cannot exceed the available quantity ({availableQuantity:0.###}).");

            // A return is modeled by lowering purchased quantity instead of adding a separate negative row.
            purchase.Quantity -= returnQuantity;
            await _materialDal.UpdatePurchaseAsync(purchase);
            return purchase;
        }

        public async Task<MaterialPurchase> RecordUsageAsync(int purchaseId, int projectId, int phaseId, decimal quantityUsed, DateOnly usageDate, string? notes)
        {
            if (quantityUsed <= 0)
                throw new ArgumentException("Used quantity must be greater than zero.");

            var purchase = await _materialDal.GetPurchaseByIdAsync(purchaseId, projectId);
            if (purchase == null)
                throw new ArgumentException("Purchase record was not found.");

            var usedQuantity = purchase.MaterialUsages.Sum(u => u.QuantityUsed);
            var availableQuantity = purchase.Quantity - usedQuantity;

            if (availableQuantity <= 0)
                throw new ArgumentException("There is no stored quantity available to use.");

            if (quantityUsed > availableQuantity)
                throw new ArgumentException($"Used quantity cannot exceed the stored quantity ({availableQuantity:0.###}).");

            // Usage is kept as its own record so inventory history remains auditable by phase and date.
            await _materialDal.AddUsageAsync(new MaterialUsage
            {
                PurchaseId = purchaseId,
                PhaseId = phaseId,
                QuantityUsed = quantityUsed,
                UsageDate = usageDate == default ? DateOnly.FromDateTime(DateTime.Today) : usageDate,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            });

            return purchase;
        }

        public async Task DeletePurchaseAsync(int purchaseId, int projectId)
        {
            await _materialDal.DeletePurchaseAsync(purchaseId, projectId);
        }
    }
}
