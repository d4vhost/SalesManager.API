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

        // Requisito 12: Reconstruir una factura completa
        public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
        {
            // FirstOrDefaultAsync puede devolver null si no encuentra la orden
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
                .Include(o => o.Customer) // Incluimos al cliente para tener su nombre
                .AsNoTracking();

            // Aplicar filtro por CustomerID (si se provee)
            if (!string.IsNullOrWhiteSpace(customerId))
            {
                query = query.Where(o => o.CustomerID == customerId);
            }

            // Aplicar filtro por EmployeeID (si se provee)
            if (employeeId.HasValue && employeeId > 0)
            {
                query = query.Where(o => o.EmployeeID == employeeId);
            }

            // Contar el total ANTES de paginar
            var totalCount = await query.CountAsync();

            // Aplicar orden y paginación
            var orders = await query
                .OrderByDescending(o => o.OrderDate) // Mostrar las más recientes primero
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, totalCount);
        }
    }
}
