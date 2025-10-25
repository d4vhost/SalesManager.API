using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.UseCases.DTOs.Products;
using SalesManager.UseCases.DTOs.Common; // <-- Añade para PagedResultDto
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Necesario para DbUpdateConcurrencyException

namespace SalesManager.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // --- MÉTODO GET MODIFICADO ---
        // GET: api/Products?searchTerm=chai&pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<ProductDto>>> GetProducts(
            [FromQuery] string searchTerm = "", // Parámetro de búsqueda (opcional)
            [FromQuery] int pageNumber = 1,    // Número de página (por defecto 1)
            [FromQuery] int pageSize = 10)     // Tamaño de página (por defecto 10)
        {
            // Validar paginación
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            // Podrías poner un límite máximo a pageSize

            // Llama al nuevo método del repositorio
            var (products, totalCount) = await _unitOfWork.ProductRepository.FindProductsAsync(searchTerm, pageNumber, pageSize);

            // Mapea a DTOs
            var productDtos = products.Select(p => new ProductDto
            {
                ProductID = p.ProductID,
                ProductName = p.ProductName,
                UnitPrice = p.UnitPrice,
                UnitsInStock = p.UnitsInStock
            }).ToList(); // Convertir a List<T> para el PagedResultDto

            // Crea el objeto de resultado paginado
            var pagedResult = new PagedResultDto<ProductDto>(productDtos, pageNumber, pageSize, totalCount);

            return Ok(pagedResult);
        }

        // GET: api/Products/sellable (Este puede quedarse igual o también paginar si lo necesitas)
        [HttpGet("sellable")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetSellableProducts()
        {
            var products = await _unitOfWork.ProductRepository.GetSellableProductsAsync();
            var productDtos = products.Select(p => new ProductDto
            {
                ProductID = p.ProductID,
                ProductName = p.ProductName,
                UnitPrice = p.UnitPrice,
                UnitsInStock = p.UnitsInStock
            });
            return Ok(productDtos);
        }

        // GET: api/Products/5 (Este no cambia)
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null) return NotFound($"Producto con ID {id} no encontrado.");
            return Ok(product);
        }

        // POST, PUT, DELETE (Estos no cambian)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Product>> PostProduct([FromBody] Product product)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            await _unitOfWork.ProductRepository.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductID }, product);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutProduct(int id, [FromBody] Product productUpdate)
        {
            if (id != productUpdate.ProductID || !ModelState.IsValid) return BadRequest();
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            product.ProductName = productUpdate.ProductName;
            product.SupplierID = productUpdate.SupplierID;
            product.CategoryID = productUpdate.CategoryID;
            product.QuantityPerUnit = productUpdate.QuantityPerUnit;
            product.UnitPrice = productUpdate.UnitPrice;
            product.UnitsInStock = productUpdate.UnitsInStock;
            product.UnitsOnOrder = productUpdate.UnitsOnOrder;
            product.ReorderLevel = productUpdate.ReorderLevel;
            product.Discontinued = productUpdate.Discontinued;

            _unitOfWork.ProductRepository.Update(product);
            try { await _unitOfWork.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (await _unitOfWork.ProductRepository.GetByIdAsync(id) == null) return NotFound();
                else return Conflict("El producto fue modificado por otro usuario.");
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            _unitOfWork.ProductRepository.Delete(product);
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }
    }
}