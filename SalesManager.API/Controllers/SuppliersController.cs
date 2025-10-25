using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.UseCases.DTOs.Common; // Para PagedResultDto
using SalesManager.UseCases.DTOs.Suppliers; // DTO que acabamos de crear
using SalesManager.UseCases.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SuppliersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _logger;

        public SuppliersController(IUnitOfWork unitOfWork, ILoggerService logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: api/Suppliers?searchTerm=...&pageNumber=...&pageSize=...
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<SupplierDto>>> GetSuppliers(
            [FromQuery] string searchTerm = "",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var (suppliers, totalCount) = await _unitOfWork.SupplierRepository.FindSuppliersAsync(searchTerm, pageNumber, pageSize);

            var supplierDtos = suppliers.Select(s => new SupplierDto
            {
                SupplierID = s.SupplierID,
                CompanyName = s.CompanyName,
                ContactName = s.ContactName,
                Phone = s.Phone,
                Country = s.Country
            }).ToList();

            var pagedResult = new PagedResultDto<SupplierDto>(supplierDtos, pageNumber, pageSize, totalCount);
            return Ok(pagedResult);
        }

        // GET: api/Suppliers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Supplier>> GetSupplier(int id)
        {
            var supplier = await _unitOfWork.SupplierRepository.GetByIdAsync(id);
            if (supplier == null)
            {
                return NotFound($"Proveedor con ID {id} no encontrado.");
            }
            return Ok(supplier);
        }

        // POST: api/Suppliers
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Supplier>> PostSupplier([FromBody] Supplier supplier)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // (Opcional: Validar si ya existe)

            await _unitOfWork.SupplierRepository.AddAsync(supplier);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInfo($"Proveedor '{supplier.CompanyName}' (ID: {supplier.SupplierID}) creado.");
            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.SupplierID }, supplier);
        }

        // PUT: api/Suppliers/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutSupplier(int id, [FromBody] Supplier supplierUpdate)
        {
            if (id != supplierUpdate.SupplierID || !ModelState.IsValid) return BadRequest();

            var supplier = await _unitOfWork.SupplierRepository.GetByIdAsync(id);
            if (supplier == null) return NotFound($"Proveedor con ID {id} no encontrado.");

            // Actualizar propiedades
            supplier.CompanyName = supplierUpdate.CompanyName;
            supplier.ContactName = supplierUpdate.ContactName;
            supplier.ContactTitle = supplierUpdate.ContactTitle;
            supplier.Address = supplierUpdate.Address;
            supplier.City = supplierUpdate.City;
            supplier.Region = supplierUpdate.Region;
            supplier.PostalCode = supplierUpdate.PostalCode;
            supplier.Country = supplierUpdate.Country;
            supplier.Phone = supplierUpdate.Phone;
            supplier.Fax = supplierUpdate.Fax;
            supplier.HomePage = supplierUpdate.HomePage;


            _unitOfWork.SupplierRepository.Update(supplier);
            try
            {
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInfo($"Proveedor ID {id} actualizado.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarn($"Conflicto de concurrencia al actualizar proveedor ID {id}.", ex);
                if (await _unitOfWork.SupplierRepository.GetByIdAsync(id) == null) return NotFound();
                else return Conflict("El proveedor fue modificado por otro usuario.");
            }
            return NoContent();
        }

        // DELETE: api/Suppliers/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _unitOfWork.SupplierRepository.GetByIdAsync(id);
            if (supplier == null) return NotFound($"Proveedor con ID {id} no encontrado.");

            // (Opcional: Validar si está en uso por algún producto)
            // var isUsed = await _context.Products.AnyAsync(p => p.SupplierID == id);
            // if (isUsed) return BadRequest("No se puede eliminar un proveedor que tiene productos asociados.");

            _unitOfWork.SupplierRepository.Delete(supplier);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInfo($"Proveedor ID {id} eliminado.");
            return NoContent();
        }
    }
}