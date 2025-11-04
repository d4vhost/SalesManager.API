// (ARCHIVO NUEVO)

using Microsoft.EntityFrameworkCore;
using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.Repositories.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.Repositories.Repositories
{
    public class EmployeeRepository : GenericRepository<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<(IReadOnlyList<Employee> Employees, int TotalCount)> FindEmployeesAsync(string searchTerm, int pageNumber, int pageSize)
        {
            // Incluimos el ApplicationUser vinculado para poder mostrar el email
            var query = _context.Employees
                .Include(e => e.ApplicationUser)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(e =>
                    (e.FirstName != null && e.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (e.LastName != null && e.LastName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (e.Title != null && e.Title.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (e.ApplicationUser != null && e.ApplicationUser.Email != null && e.ApplicationUser.Email.Contains(term, StringComparison.OrdinalIgnoreCase))
                );
            }

            var totalCount = await query.CountAsync();

            var employees = await query
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (employees, totalCount);
        }
    }
}