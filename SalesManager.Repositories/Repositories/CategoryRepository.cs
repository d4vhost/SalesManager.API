using Microsoft.EntityFrameworkCore; // Para LINQ async
using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.Repositories.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.Repositories.Repositories
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext context) : base(context)
        {
        }

        // Implementación de búsqueda y paginación para Categorías
        public async Task<(IReadOnlyList<Category> Categories, int TotalCount)> FindCategoriesAsync(string searchTerm, int pageNumber, int pageSize)
        {
            var query = _context.Categories.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(c =>
                    c.CategoryName.ToLower().Contains(term) ||
                    (c.Description != null && c.Description.ToLower().Contains(term))
                );
            }

            var totalCount = await query.CountAsync();

            var categories = await query
                .OrderBy(c => c.CategoryName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (categories, totalCount);
        }
    }
}