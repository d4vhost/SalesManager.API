using Microsoft.AspNetCore.Identity;
using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.UseCases.DTOs.Employee;
using SalesManager.UseCases.Interfaces;
using System.Text;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SalesManager.UseCases.Features
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILoggerService _logger;

        public EmployeeService(
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager,
            ILoggerService logger)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<int> CreateEmployeeAndUserAsync(CreateEmployeeRequestDto dto)
        {
            // 1. Validar si el email ya existe
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("Ya existe un usuario con ese correo electrónico.");
            }

            // 2. Iniciar transacción
            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 3. Crear la entidad Empleado
                var employee = new Employee
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Title = dto.Title,
                    HomePhone = dto.Phone,
                    Address = dto.Address,
                    City = dto.City,
                    Country = dto.Country,
                    HireDate = DateTime.UtcNow
                };

                // 4. Guardar Empleado para obtener su ID
                await _unitOfWork.EmployeeRepository.AddAsync(employee);
                await _unitOfWork.SaveChangesAsync(); // Guarda en la BD y obtiene el EmployeeID

                // 5. Crear la entidad ApplicationUser (Login)
                var user = new ApplicationUser
                {
                    UserName = dto.Email,
                    Email = dto.Email,
                    Nombre = dto.FirstName,
                    Apellido = dto.LastName,
                    Cedula = dto.Cedula,
                    EmployeeID = employee.EmployeeID, // <-- VINCULARLOS
                    EmailConfirmed = true
                };

                // 6. Crear el usuario en Identity
                var identityResult = await _userManager.CreateAsync(user, dto.Password);
                if (!identityResult.Succeeded)
                {
                    // --- INICIO DE MODIFICACIÓN ---
                    // NO hacer rollback aquí. Solo lanzar la excepción.
                    // El bloque 'catch' se encargará del rollback.
                    // await transaction.RollbackAsync(); // <-- LÍNEA ELIMINADA
                    throw new InvalidOperationException($"Error al crear el login: {GetIdentityErrors(identityResult)}");
                    // --- FIN DE MODIFICACIÓN ---
                }

                // 7. Asignar rol "Usuario" (rol de ventas/POS)
                var roleResult = await _userManager.AddToRoleAsync(user, "Usuario");
                if (!roleResult.Succeeded)
                {
                    // --- INICIO DE MODIFICACIÓN ---
                    // NO hacer rollback aquí. Solo lanzar la excepción.
                    // await transaction.RollbackAsync(); // <-- LÍNEA ELIMINADA
                    throw new InvalidOperationException($"Error al asignar rol: {GetIdentityErrors(roleResult)}");
                    // --- FIN DE MODIFICACIÓN ---
                }

                // 8. Confirmar la transacción
                await transaction.CommitAsync();

                _logger.LogInfo($"Nuevo empleado y usuario creados. EmployeeID: {employee.EmployeeID}, User: {user.Email}");
                return employee.EmployeeID;
            }
            catch (Exception ex)
            {
                // --- MODIFICACIÓN ---
                // Este bloque 'catch' ahora manejará TODOS los rollbacks.
                await transaction.RollbackAsync();
                // --- FIN MODIFICACIÓN ---

                _logger.LogError($"Error en CreateEmployeeAndUserAsync: {ex.Message}", ex);
                throw; // Re-lanza la excepción
            }
        }

        public async Task UpdateEmployeeAsync(int employeeId, UpdateEmployeeDto dto)
        {
            var employee = await _unitOfWork.EmployeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
                throw new InvalidOperationException("Empleado no encontrado.");

            // Actualizar datos del empleado
            employee.FirstName = dto.FirstName;
            employee.LastName = dto.LastName;
            employee.Title = dto.Title;
            employee.HomePhone = dto.Phone;
            employee.Address = dto.Address;
            employee.City = dto.City;
            employee.Country = dto.Country;

            _unitOfWork.EmployeeRepository.Update(employee);

            // También actualizamos el ApplicationUser por consistencia
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeID == employeeId);
            if (user != null)
            {
                user.Nombre = dto.FirstName;
                user.Apellido = dto.LastName;
                await _userManager.UpdateAsync(user);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteEmployeeAndUserAsync(int employeeId)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var employee = await _unitOfWork.EmployeeRepository.GetByIdAsync(employeeId);
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeID == employeeId);

                if (user != null)
                {
                    var identityResult = await _userManager.DeleteAsync(user);
                    if (!identityResult.Succeeded)
                    {
                        // --- INICIO DE MODIFICACIÓN ---
                        // (Misma lógica que en Create)
                        // await transaction.RollbackAsync(); // <-- LÍNEA ELIMINADA
                        throw new InvalidOperationException($"Error al eliminar el login: {GetIdentityErrors(identityResult)}");
                        // --- FIN DE MODIFICACIÓN ---
                    }
                }

                if (employee != null)
                {
                    _unitOfWork.EmployeeRepository.Delete(employee);
                    await _unitOfWork.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error en DeleteEmployeeAndUserAsync: {ex.Message}", ex);
                // Captura específica de error de FK (si un empleado tiene órdenes)
                if (ex.InnerException != null && ex.InnerException.Message.Contains("REFERENCE constraint"))
                {
                    throw new InvalidOperationException("No se puede eliminar el empleado porque tiene órdenes asociadas. (Error de FK)");
                }
                throw;
            }
        }

        private string GetIdentityErrors(IdentityResult result)
        {
            return string.Join(", ", result.Errors.Select(e => e.Description));
        }
    }
}