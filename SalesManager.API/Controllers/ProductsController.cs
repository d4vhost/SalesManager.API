using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManager.BusinessObjects.Entities; // Necesario para Product
using SalesManager.BusinessObjects.Interfaces; // Necesario para IUnitOfWork
using SalesManager.UseCases.DTOs.Products; // Necesario para ProductDto
using System.Collections.Generic;
using System.Linq; // Necesario para .Select()
using System.Threading.Tasks; // Necesario para Task<>
using Microsoft.EntityFrameworkCore; // Necesario para DbUpdateConcurrencyException

namespace SalesManager.WebAPI.Controllers // Asegúrate que el namespace sea correcto
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requiere autenticación para todas las acciones
    public class ProductsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            var products = await _unitOfWork.ProductRepository.GetAllAsync();
            // Mapeo simple a DTO
            var productDtos = products.Select(p => new ProductDto
            {
                ProductID = p.ProductID,
                ProductName = p.ProductName,
                UnitPrice = p.UnitPrice,
                UnitsInStock = p.UnitsInStock
            });
            return Ok(productDtos);
        }

        // GET: api/Products/sellable
        // Requisito 8: Mostrar solo productos con stock > 0.pdf"]
        [HttpGet("sellable")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetSellableProducts()
        {
            // Llama al método específico del repositorio
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

        // GET: api/Products/{id} (ej: /api/Products/5)
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id) // Devuelve la entidad completa
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);

            if (product == null)
            {
                return NotFound($"Producto con ID {id} no encontrado.");
            }
            return Ok(product);
        }

        // POST: api/Products
        [HttpPost]
        [Authorize(Roles = "Admin")] // Solo Admins pueden crear productos.pdf"]
        public async Task<ActionResult<Product>> PostProduct([FromBody] Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // (Opcional: Validar si ya existe un producto con el mismo nombre, etc.)

            await _unitOfWork.ProductRepository.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            // Devuelve el objeto creado y la ruta para obtenerlo
            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductID }, product);
        }

        // PUT: api/Products/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Solo Admins pueden modificar productos
        public async Task<IActionResult> PutProduct(int id, [FromBody] Product productUpdate)
        {
            if (id != productUpdate.ProductID || !ModelState.IsValid)
            {
                return BadRequest("El ID proporcionado no coincide o el modelo no es válido.");
            }

            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound($"Producto con ID {id} no encontrado.");
            }

            // Actualiza las propiedades
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

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) // Manejo básico de concurrencia.pdf"]
            {
                if (await _unitOfWork.ProductRepository.GetByIdAsync(id) == null)
                {
                    return NotFound();
                }
                else
                {
                    return Conflict("El producto fue modificado por otro usuario. Recargue los datos.");
                }
            }

            return NoContent(); // Éxito
        }

        // DELETE: api/Products/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Solo Admins pueden eliminar productos
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _unitOfWork.ProductRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound($"Producto con ID {id} no encontrado.");
            }

            // (Opcional) Validación: No permitir borrar si está en alguna orden
            // var isInOrder = await _context.OrderDetails.AnyAsync(od => od.ProductID == id);
            // if (isInOrder) return BadRequest("No se puede eliminar un producto que está en órdenes.");

            _unitOfWork.ProductRepository.Delete(product);
            await _unitOfWork.SaveChangesAsync();

            return NoContent(); // Éxito
        }
    }
}