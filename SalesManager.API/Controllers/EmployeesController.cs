using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.UseCases.DTOs.Common;
using SalesManager.UseCases.DTOs.Employee;
using SalesManager.UseCases.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Solo Admins pueden gestionar empleados
    public class EmployeesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmployeeService _employeeService;
        private readonly ILoggerService _logger;

        public EmployeesController(
            IUnitOfWork unitOfWork,
            IEmployeeService employeeService,
            ILoggerService logger)
        {
            _unitOfWork = unitOfWork;
            _employeeService = employeeService;
            _logger = logger;
        }

        // GET: api/Employees?searchTerm=...
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<EmployeeDto>>> GetEmployees(
            [FromQuery] string searchTerm = "",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var (employees, totalCount) = await _unitOfWork.EmployeeRepository.FindEmployeesAsync(searchTerm, pageNumber, pageSize);

            // Mapear al DTO
            var employeeDtos = employees.Select(e => new EmployeeDto
            {
                EmployeeID = e.EmployeeID,
                FullName = $"{e.FirstName} {e.LastName}",
                Title = e.Title,
                Phone = e.HomePhone,
                // Datos del usuario vinculado
                Email = e.ApplicationUser?.Email,
                IsLockedOut = e.ApplicationUser != null && e.ApplicationUser.LockoutEnd.HasValue && e.ApplicationUser.LockoutEnd > DateTimeOffset.UtcNow
            }).ToList();

            var pagedResult = new PagedResultDto<EmployeeDto>(employeeDtos, pageNumber, pageSize, totalCount);
            return Ok(pagedResult);
        }

        // GET: api/Employees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(int id)
        {
            var employee = await _unitOfWork.EmployeeRepository.GetByIdAsync(id);
            if (employee == null)
            {
                return NotFound($"Empleado con ID {id} no encontrado.");
            }
            return Ok(employee);
        }

        // POST: api/Employees
        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var newEmployeeId = await _employeeService.CreateEmployeeAndUserAsync(dto);
                return CreatedAtAction(nameof(GetEmployee), new { id = newEmployeeId }, new { employeeId = newEmployeeId });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarn(ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error al crear empleado y usuario.", ex);
                return StatusCode(500, new { message = "Error interno del servidor." });
            }
        }

        // PUT: api/Employees/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _employeeService.UpdateEmployeeAsync(id, dto);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al actualizar empleado {id}.", ex);
                return StatusCode(500, new { message = "Error interno del servidor." });
            }
        }

        // DELETE: api/Employees/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                await _employeeService.DeleteEmployeeAndUserAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                // Puede ser FK (órdenes) o que no se encontró
                _logger.LogWarn(ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al eliminar empleado {id}.", ex);
                return StatusCode(500, new { message = "Error interno del servidor." });
            }
        }
    }
}