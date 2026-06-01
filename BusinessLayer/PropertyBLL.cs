using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using BuildWise.Models;
using BuildWise.DataLayer;

namespace BuildWise.BusinessLayer
{
    public class PropertyBLL
    {
        private readonly PropertyDAL _propertyDal;

        public PropertyBLL(PropertyDAL propertyDal)
        {
            _propertyDal = propertyDal;
        }

        public async Task<List<Property>> GetUserPropertiesAsync(int userId, int? projectId = null)
        {
            return await _propertyDal.GetPropertiesByUserIdAsync(userId, projectId);
        }

        public async Task<Property?> GetPropertyDetailsAsync(int propertyId, int userId)
        {
            return await _propertyDal.GetPropertyByIdAsync(propertyId, userId);
        }

        public async Task AddPropertyAsync(Property property)
        {
            if (string.IsNullOrWhiteSpace(property.PropertyName))
                throw new ArgumentException("Property name is required.");
            if (property.AreaSize <= 0)
                throw new ArgumentException("Area size must be greater than zero.");
            
            await _propertyDal.AddPropertyAsync(property);
        }

        public async Task UpdatePropertyAsync(Property property, int currentUserId)
        {
            var existing = await _propertyDal.GetPropertyByIdAsync(property.PropertyId, currentUserId);
            if (existing == null)
                throw new UnauthorizedAccessException("Property not found or access denied.");

            existing.PropertyName = property.PropertyName;
            existing.TypeId = property.TypeId;
            existing.StatusId = property.StatusId;
            existing.ProjectId = property.ProjectId;
            existing.Location = property.Location;
            existing.City = property.City;
            existing.AreaSize = property.AreaSize;
            existing.AreaUnitId = property.AreaUnitId;
            existing.Notes = property.Notes;

            await _propertyDal.UpdatePropertyAsync(existing);
        }

        public async Task DeletePropertyAsync(int propertyId, int userId)
        {
            await _propertyDal.DeletePropertyAsync(propertyId, userId);
        }
    }
}
