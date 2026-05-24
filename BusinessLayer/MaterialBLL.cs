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

            purchase.Quantity -= returnQuantity;
            await _materialDal.UpdatePurchaseAsync(purchase);
            return purchase;
        }

        public async Task DeletePurchaseAsync(int purchaseId, int projectId)
        {
            await _materialDal.DeletePurchaseAsync(purchaseId, projectId);
        }
    }
}
