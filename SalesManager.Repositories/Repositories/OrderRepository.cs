using Microsoft.EntityFrameworkCore;
using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.Repositories.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.Repositories.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Customer)             
                .Include(o => o.OrderDetails)         
                    .ThenInclude(od => od.Product)    
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderID == orderId);
        }

        public async Task<(IReadOnlyList<Order> Orders, int TotalCount)> FindOrdersAsync(
            int pageNumber,
            int pageSize,
            string? customerId,
            int? employeeId)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(customerId))
            {
                query = query.Where(o => o.CustomerID == customerId);
            }

            if (employeeId.HasValue && employeeId > 0)
            {
                query = query.Where(o => o.EmployeeID == employeeId);
            }

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }
    }
}