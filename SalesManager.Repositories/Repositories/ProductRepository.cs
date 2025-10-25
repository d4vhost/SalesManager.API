using SalesManager.Repositories.Persistence;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.BusinessObjects.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.Repositories.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context)
        {
        }

        // Requisito 8: Mostrar solo productos con stock > 0
        public async Task<IReadOnlyList<Product>> GetSellableProductsAsync()
        {
            return await _context.Products
                .Where(p => p.UnitsInStock > 0 && !p.Discontinued)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
