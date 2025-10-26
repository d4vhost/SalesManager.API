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
            // Este método sigue igual, útil para casos sin paginación/búsqueda
            return await _context.Products
                .Where(p => p.UnitsInStock > 0 && !p.Discontinued)
                .AsNoTracking()
                .ToListAsync();
        }

        // --- IMPLEMENTACIÓN DEL MÉTODO MODIFICADO ---
        public async Task<(IReadOnlyList<Product> Products, int TotalCount)> FindProductsAsync(string searchTerm, int pageNumber, int pageSize, int? categoryId = null) // Añadido categoryId
        {
            var query = _context.Products.AsNoTracking();

            // Aplicamos el filtro de búsqueda si searchTerm no está vacío
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                // Requisito 15: "búsqueda inteligente que busque por todos los campos" (o los más relevantes).pdf"]
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(term) ||
                    (p.ProductID.ToString() == term) // Permitir buscar por ID exacto
                );
            }

            // --- AÑADIR FILTRO POR CATEGORÍA ---
            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(p => p.CategoryID == categoryId);
            }
            // --- FIN AÑADIR FILTRO ---

            // --- AÑADIR FILTRO PARA SOLO VENDIBLES ---
            // Requisito 8.pdf"]
            query = query.Where(p => p.UnitsInStock > 0 && !p.Discontinued);
            // --- FIN FILTRO VENDIBLES ---


            // Calculamos el total de registros ANTES de paginar (después de aplicar filtros)
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