using SalesManager.BusinessObjects.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SalesManager.BusinessObjects.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<Order?> GetOrderWithDetailsAsync(int orderId);
        Task<(IReadOnlyList<Order> Orders, int TotalCount)> FindOrdersAsync(
            int pageNumber,
            int pageSize,
            string? customerId,
            int? employeeId);
    }
}