using Microsoft.EntityFrameworkCore;
using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.Repositories.Persistence;
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
    }
}