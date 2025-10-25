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

        public async Task<IReadOnlyList<Product>> GetSellableProductsAsync()
        {
            return await _context.Products
                .Where(p => p.UnitsInStock > 0 && !p.Discontinued)
                .AsNoTracking()
                .ToListAsync();
        }

        // --- IMPLEMENTACIÓN DEL NUEVO MÉTODO ---
        public async Task<(IReadOnlyList<Product> Products, int TotalCount)> FindProductsAsync(string searchTerm, int pageNumber, int pageSize)
        {
            // Empezamos con la consulta base
            var query = _context.Products.AsNoTracking();

            // Aplicamos el filtro de búsqueda si searchTerm no está vacío
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                // Requisito 15: "búsqueda inteligente que busque por todos los campos" (o los más relevantes).pdf"]
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(term) ||
                    (p.ProductID.ToString() == term) // Permitir buscar por ID exacto
                                                     // Añade más campos si quieres buscar por ellos (ej: p.Category.CategoryName)
                );
            }

            // Calculamos el total de registros ANTES de paginar
            var totalCount = await query.CountAsync();

            // Aplicamos la paginación
            var products = await query
                .OrderBy(p => p.ProductName) // Es bueno ordenar antes de paginar
                .Skip((pageNumber - 1) * pageSize) // Saltar registros de páginas anteriores
                .Take(pageSize) // Tomar solo los de la página actual
                .ToListAsync();

            return (products, totalCount);
        }
    }
}
