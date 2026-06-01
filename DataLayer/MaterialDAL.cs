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
                .AsNoTracking()
                .Include(m => m.DefaultUnit)
                .Where(m => (m.UserId == 1 && m.IsActive) || m.UserId == userId)
                .OrderBy(m => m.MaterialName)
                .ToListAsync();
        }

        public async Task<Material?> GetMaterialByIdAsync(int materialId, int userId)
        {
            return await _context.Materials
                .Include(m => m.DefaultUnit)
                .FirstOrDefaultAsync(m => m.MaterialId == materialId && m.UserId == userId);
        }

        public async Task AddMaterialAsync(Material material)
        {
            _context.Materials.Add(material);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMaterialAsync(Material material)
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<MaterialPurchase>> GetPurchasesByProjectIdAsync(int projectId)
        {
            return await _context.MaterialPurchases
                .AsNoTracking()
                .Include(mp => mp.Material)
                .Include(mp => mp.Unit)
                .Include(mp => mp.Supplier)
                .Include(mp => mp.MaterialUsages)
                    .ThenInclude(mu => mu.Phase)
                        .ThenInclude(p => p.PhaseType)
                .Include(mp => mp.MaterialUsages)
                    .ThenInclude(mu => mu.Phase)
                        .ThenInclude(p => p.Property)
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
                .Include(mp => mp.MaterialUsages)
                .FirstOrDefaultAsync(mp => mp.PurchaseId == purchaseId && mp.ProjectId == projectId);
        }

        public async Task AddPurchaseAsync(MaterialPurchase purchase)
        {
            purchase.CreatedAt = System.DateTime.UtcNow;
            _context.MaterialPurchases.Add(purchase);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePurchaseAsync(MaterialPurchase purchase)
        {
            await _context.SaveChangesAsync();
        }

        public async Task RemovePurchaseAsync(MaterialPurchase purchase)
        {
            _context.MaterialUsages.RemoveRange(purchase.MaterialUsages);
            _context.MaterialPurchases.Remove(purchase);
            await _context.SaveChangesAsync();
        }

        public async Task AddUsageAsync(MaterialUsage usage)
        {
            _context.MaterialUsages.Add(usage);
            await _context.SaveChangesAsync();
        }
        
        public async Task DeletePurchaseAsync(int purchaseId, int projectId)
        {
            var purchase = await GetPurchaseByIdAsync(purchaseId, projectId);
            if (purchase != null)
            {
                _context.MaterialUsages.RemoveRange(purchase.MaterialUsages);
                _context.MaterialPurchases.Remove(purchase);
                await _context.SaveChangesAsync();
            }
        }
    }
}
