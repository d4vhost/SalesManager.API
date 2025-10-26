using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.UseCases.DTOs.Products;
using SalesManager.UseCases.DTOs.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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

        // GET: api/Products?searchTerm=chai&pageNumber=1&pageSize=10&categoryId=1
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<ProductDto>>> GetProducts(
            [FromQuery] string searchTerm = "",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? categoryId = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var (products, totalCount) = await _unitOfWork.ProductRepository.FindProductsAsync(searchTerm, pageNumber, pageSize, categoryId);

            // --- ACTUALIZAR MAPEADO ---
            var productDtos = products.Select(p => new ProductDto
            {
                ProductID = p.ProductID,
                ProductName = p.ProductName,
                UnitPrice = p.UnitPrice,
                UnitsInStock = p.UnitsInStock,
                QuantityPerUnit = p.QuantityPerUnit, // Añadido
                Discontinued = p.Discontinued        // Añadido
            }).ToList();
            // --- FIN ACTUALIZACIÓN ---

            var pagedResult = new PagedResultDto<ProductDto>(productDtos, pageNumber, pageSize, totalCount);

            return Ok(pagedResult);
        }

        // GET: api/Products/sellable
        [HttpGet("sellable")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetSellableProducts()
        {
            var products = await _unitOfWork.ProductRepository.GetSellableProductsAsync();
            // --- ACTUALIZAR MAPEADO ---
            var productDtos = products.Select(p => new ProductDto
            {
                ProductID = p.ProductID,
                ProductName = p.ProductName,
                UnitPrice = p.UnitPrice,
                UnitsInStock = p.UnitsInStock,
                QuantityPerUnit = p.QuantityPerUnit, // Añadido
                Discontinued = p.Discontinued        // Añadido
            });
            // --- FIN ACTUALIZACIÓN ---
            return Ok(productDtos);
        }

        // GET: api/Products/5 (No cambia)
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null) return NotFound($"Producto con ID {id} no encontrado.");
            return Ok(product); // Devuelve entidad completa, frontend puede usar lo que necesite
        }

        // POST, PUT, DELETE (No cambian)
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