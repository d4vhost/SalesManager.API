using Microsoft.EntityFrameworkCore;
using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.Repositories.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.Repositories.Repositories
{
    public class SupplierRepository : GenericRepository<Supplier>, ISupplierRepository
    {
        public SupplierRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<(IReadOnlyList<Supplier> Suppliers, int TotalCount)> FindSuppliersAsync(string searchTerm, int pageNumber, int pageSize)
        {
            var query = _context.Suppliers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(s =>
                    s.CompanyName.ToLower().Contains(term) ||
                    (s.ContactName != null && s.ContactName.ToLower().Contains(term)) ||
                    (s.Country != null && s.Country.ToLower().Contains(term))
                );
            }

            var totalCount = await query.CountAsync();

            var suppliers = await query
                .OrderBy(s => s.CompanyName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (suppliers, totalCount);
        }
    }
}