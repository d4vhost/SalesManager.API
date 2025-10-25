using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManager.BusinessObjects.Entities; // Necesario para Customer
using SalesManager.BusinessObjects.Interfaces; // Necesario para IUnitOfWork
using SalesManager.UseCases.DTOs.Customers; // Necesario para CustomerDto
using System.Collections.Generic;
using System.Linq; // Necesario para .Select()
using System.Threading.Tasks; // Necesario para Task<>
using Microsoft.EntityFrameworkCore; // Necesario para DbUpdateConcurrencyException

namespace SalesManager.WebAPI.Controllers // Asegúrate que el namespace sea correcto
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requiere autenticación para todas las acciones
    public class CustomersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CustomersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/Customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers()
        {
            var customers = await _unitOfWork.CustomerRepository.GetAllAsync();
            // Mapeo simple a DTO (podrías usar AutoMapper aquí)
            var customerDtos = customers.Select(c => new CustomerDto
            {
                CustomerID = c.CustomerID,
                CompanyName = c.CompanyName,
                ContactName = c.ContactName,
                Phone = c.Phone
                // Añade más campos si los necesitas en la vista general
            });
            return Ok(customerDtos);
        }

        // GET: api/Customers/{id} (ej: /api/Customers/ALFKI)
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(string id) // El ID de Customer es string
        {
            var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(id);

            if (customer == null)
            {
                return NotFound($"Cliente con ID {id} no encontrado.");
            }
            // Aquí devolvemos la entidad completa, podrías mapear a un DTO más detallado si prefieres
            return Ok(customer);
        }

        // POST: api/Customers
        [HttpPost]
        [Authorize(Roles = "Admin")] // Solo Admins pueden crear clientes.pdf"]
        public async Task<ActionResult<Customer>> PostCustomer([FromBody] Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validar si el CustomerID ya existe
            var existingCustomer = await _unitOfWork.CustomerRepository.GetByIdAsync(customer.CustomerID);
            if (existingCustomer != null)
            {
                return Conflict($"Ya existe un cliente con el ID '{customer.CustomerID}'.");
            }

            await _unitOfWork.CustomerRepository.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            // Devuelve el objeto creado y la ruta para obtenerlo (código 201 Created)
            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerID }, customer);
        }

        // PUT: api/Customers/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Solo Admins pueden modificar clientes
        public async Task<IActionResult> PutCustomer(string id, [FromBody] Customer customerUpdate)
        {
            // Valida que el ID de la ruta coincida con el ID en el cuerpo
            if (id != customerUpdate.CustomerID || !ModelState.IsValid)
            {
                return BadRequest("El ID proporcionado no coincide o el modelo no es válido.");
            }

            // Obtén la entidad existente desde la base de datos
            var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound($"Cliente con ID {id} no encontrado.");
            }

            // Actualiza las propiedades de la entidad existente con los valores del DTO recibido
            customer.CompanyName = customerUpdate.CompanyName;
            customer.ContactName = customerUpdate.ContactName;
            customer.ContactTitle = customerUpdate.ContactTitle;
            customer.Address = customerUpdate.Address;
            customer.City = customerUpdate.City;
            customer.Region = customerUpdate.Region;
            customer.PostalCode = customerUpdate.PostalCode;
            customer.Country = customerUpdate.Country;
            customer.Phone = customerUpdate.Phone;
            customer.Fax = customerUpdate.Fax;
            // No actualizamos CustomerID porque es la clave primaria

            _unitOfWork.CustomerRepository.Update(customer);

            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) // Manejo básico de concurrencia.pdf"]
            {
                if (await _unitOfWork.CustomerRepository.GetByIdAsync(id) == null)
                {
                    return NotFound(); // El cliente fue eliminado mientras editábamos
                }
                else
                {
                    return Conflict("El cliente fue modificado por otro usuario. Recargue los datos.");
                }
            }

            // Devuelve 204 No Content si la actualización fue exitosa
            return NoContent();
        }

        // DELETE: api/Customers/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Solo Admins pueden eliminar clientes
        public async Task<IActionResult> DeleteCustomer(string id)
        {
            var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound($"Cliente con ID {id} no encontrado.");
            }

            // (Opcional) Validación de negocio: No permitir borrar si tiene órdenes asociadas
            // var hasOrders = await _context.Orders.AnyAsync(o => o.CustomerID == id); // Necesitarías inyectar DbContext o tener método en repo
            // if (hasOrders) return BadRequest("No se puede eliminar un cliente que tiene órdenes.");

            _unitOfWork.CustomerRepository.Delete(customer);
            await _unitOfWork.SaveChangesAsync();

            // Devuelve 204 No Content si la eliminación fue exitosa
            return NoContent();
        }
    }
}