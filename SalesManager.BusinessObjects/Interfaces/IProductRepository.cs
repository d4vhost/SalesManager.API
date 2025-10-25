using SalesManager.BusinessObjects.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.BusinessObjects.Interfaces
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        // Requisito 8: "Los productos para vender... stock mayor que cero"
        //.pdf", source 31]
        Task<IReadOnlyList<Product>> GetSellableProductsAsync();
    }
}
