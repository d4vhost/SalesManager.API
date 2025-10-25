using SalesManager.BusinessObjects.Entities;
using System.Threading.Tasks;

namespace SalesManager.BusinessObjects.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<Order?> GetOrderWithDetailsAsync(int orderId); // <-- Añade '?'
    }
}