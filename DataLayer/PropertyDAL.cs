using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Microsoft.EntityFrameworkCore;
using BuildWise.Models;

namespace BuildWise.DataLayer
{
    public class PropertyDAL
    {
        private readonly BuildWiseDbContext _context;

        public PropertyDAL(BuildWiseDbContext context)
        {
            _context = context;
        }

        public async Task<List<Property>> GetPropertiesByUserIdAsync(int userId)
        {
            return await _context.Properties
                .Include(p => p.Type)
                .Include(p => p.Status)
                .Include(p => p.AreaUnit)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Property?> GetPropertyByIdAsync(int propertyId, int userId)
        {
            return await _context.Properties
                .Include(p => p.Type)
                .Include(p => p.Status)
                .Include(p => p.AreaUnit)
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.UserId == userId);
        }

        public async Task AddPropertyAsync(Property property)
        {
            property.CreatedAt = System.DateTime.UtcNow;
            property.UpdatedAt = System.DateTime.UtcNow;
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePropertyAsync(Property property)
        {
            property.UpdatedAt = System.DateTime.UtcNow;
            _context.Properties.Update(property);
            await _context.SaveChangesAsync();
        }

        public async Task DeletePropertyAsync(int propertyId, int userId)
        {
            var property = await GetPropertyByIdAsync(propertyId, userId);
            if (property != null)
            {
                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();
            }
        }
    }
}
