using Microsoft.EntityFrameworkCore;
using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.Repositories.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.Repositories.Repositories
{
    public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(ApplicationDbContext context) : base(context)
        {
        }

        // --- IMPLEMENTACIÓN DEL NUEVO MÉTODO ---
        public async Task<(IReadOnlyList<Customer> Customers, int TotalCount)> FindCustomersAsync(string searchTerm, int pageNumber, int pageSize)
        {
            var query = _context.Customers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                // Requisito 15: Búsqueda por múltiples campos.pdf"]
                query = query.Where(c =>
                    c.CompanyName.ToLower().Contains(term) ||
                    (c.ContactName != null && c.ContactName.ToLower().Contains(term)) ||
                    c.CustomerID.ToLower() == term || // Buscar por ID exacto
                    (c.Country != null && c.Country.ToLower().Contains(term)) ||
                    (c.City != null && c.City.ToLower().Contains(term))
                );
            }

            var totalCount = await query.CountAsync();

            var customers = await query
                .OrderBy(c => c.CompanyName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (customers, totalCount);
        }
    }
}
