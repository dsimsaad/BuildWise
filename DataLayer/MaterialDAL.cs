using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Microsoft.EntityFrameworkCore;
using BuildWise.Models;

namespace BuildWise.DataLayer
{
    public class MaterialDAL
    {
        private readonly BuildWiseDbContext _context;

        public MaterialDAL(BuildWiseDbContext context)
        {
            _context = context;
        }

        public async Task<List<Material>> GetMaterialsByUserIdAsync(int userId)
        {
            return await _context.Materials
                .Include(m => m.DefaultUnit)
                .Where(m => (m.UserId == userId || m.UserId == 1) && m.IsActive)
                .OrderBy(m => m.MaterialName)
                .ToListAsync();
        }

        public async Task AddMaterialAsync(Material material)
        {
            _context.Materials.Add(material);
            await _context.SaveChangesAsync();
        }

        public async Task<List<MaterialPurchase>> GetPurchasesByProjectIdAsync(int projectId)
        {
            return await _context.MaterialPurchases
                .Include(mp => mp.Material)
                .Include(mp => mp.Unit)
                .Include(mp => mp.Supplier)
                .Include(mp => mp.MaterialUsages)
                .Where(mp => mp.ProjectId == projectId)
                .OrderByDescending(mp => mp.PurchaseDate)
                .ToListAsync();
        }

        public async Task<MaterialPurchase?> GetPurchaseByIdAsync(int purchaseId, int projectId)
        {
            return await _context.MaterialPurchases
                .Include(mp => mp.Material)
                .Include(mp => mp.Unit)
                .Include(mp => mp.Supplier)
                .FirstOrDefaultAsync(mp => mp.PurchaseId == purchaseId && mp.ProjectId == projectId);
        }

        public async Task AddPurchaseAsync(MaterialPurchase purchase)
        {
            purchase.CreatedAt = System.DateTime.UtcNow;
            purchase.TotalCost = purchase.Quantity * purchase.UnitPrice;
            _context.MaterialPurchases.Add(purchase);
            await _context.SaveChangesAsync();
        }
        
        public async Task DeletePurchaseAsync(int purchaseId, int projectId)
        {
            var purchase = await GetPurchaseByIdAsync(purchaseId, projectId);
            if (purchase != null)
            {
                _context.MaterialPurchases.Remove(purchase);
                await _context.SaveChangesAsync();
            }
        }
    }
}
