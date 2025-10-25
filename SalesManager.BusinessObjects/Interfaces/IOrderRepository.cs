using SalesManager.BusinessObjects.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.BusinessObjects.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        // Requisito 12: "muestre el encabezado y el detalle de la ORDEN DE VENTAS"
        //.pdf", source 31]
        Task<Order> GetOrderWithDetailsAsync(int orderId);
    }
}
