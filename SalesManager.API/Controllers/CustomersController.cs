using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.UseCases.DTOs.Customers;
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
    public class CustomersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CustomersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: api/Customers?searchTerm=maria&pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<CustomerDto>>> GetCustomers(
            [FromQuery] string searchTerm = "",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var (customers, totalCount) = await _unitOfWork.CustomerRepository.FindCustomersAsync(searchTerm, pageNumber, pageSize);

            // --- ACTUALIZAR MAPEADO ---
            var customerDtos = customers.Select(c => new CustomerDto
            {
                CustomerID = c.CustomerID,
                CompanyName = c.CompanyName ?? "",
                ContactName = c.ContactName,
                Phone = c.Phone,
                Address = c.Address, 
                City = c.City,      
                Country = c.Country  
            }).ToList();
            // --- FIN ACTUALIZACIÓN ---

            var pagedResult = new PagedResultDto<CustomerDto>(customerDtos, pageNumber, pageSize, totalCount);

            return Ok(pagedResult);
        }

        // GET: api/Customers/ALFKI (No cambia)
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(string id)
        {
            var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(id);
            if (customer == null) return NotFound($"Cliente con ID {id} no encontrado.");
            return Ok(customer);
        }

        // POST, PUT, DELETE (Estos no cambian)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Customer>> PostCustomer([FromBody] Customer customer)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var existingCustomer = await _unitOfWork.CustomerRepository.GetByIdAsync(customer.CustomerID);
            if (existingCustomer != null) return Conflict($"Ya existe un cliente con el ID '{customer.CustomerID}'.");
            await _unitOfWork.CustomerRepository.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerID }, customer);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutCustomer(string id, [FromBody] Customer customerUpdate)
        {
            if (id != customerUpdate.CustomerID || !ModelState.IsValid) return BadRequest();
            var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(id);
            if (customer == null) return NotFound();

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

            _unitOfWork.CustomerRepository.Update(customer);
            try { await _unitOfWork.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException)
            {
                if (await _unitOfWork.CustomerRepository.GetByIdAsync(id) == null) return NotFound();
                else return Conflict("El cliente fue modificado por otro usuario.");
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCustomer(string id)
        {
            var customer = await _unitOfWork.CustomerRepository.GetByIdAsync(id);
            if (customer == null) return NotFound();
            _unitOfWork.CustomerRepository.Delete(customer);
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }
    }
}