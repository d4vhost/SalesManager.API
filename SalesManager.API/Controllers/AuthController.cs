using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SalesManager.BusinessObjects.Entities;
using SalesManager.UseCases.DTOs.Auth;
using SalesManager.UseCases.Interfaces;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Necesario para ToListAsync() en UserManager.Users
using System.Collections.Generic; // Necesario para List<>
using System.Linq; // Necesario para Select()

namespace SalesManager.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILoggerService _logger; // Añadido para logging

        public AuthController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILoggerService logger) // Inyecta el logger
        {
            _authService = authService;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger; // Asigna el logger
        }

        // --- Endpoints de Autenticación (Login, Register) ---

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto loginRequest)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.LoginAsync(loginRequest);
            if (!result.IsSuccess) return Unauthorized(new { message = result.ErrorMessage });
            return Ok(result);
        }

        [HttpPost("register")]
        [AllowAnonymous] // O [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequestDto registerRequest)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.RegisterAsync(registerRequest, "Usuario");
            if (!result.IsSuccess) return BadRequest(new { message = result.ErrorMessage });
            return Ok(new { message = "Usuario registrado exitosamente." });
        }

        // --- Endpoints de Gestión de Roles ---

        [HttpPost("roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRole([FromBody] string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return BadRequest("El nombre del rol es requerido.");
            // Asegura que los roles base existan
            if (!await _roleManager.RoleExistsAsync("Admin")) await _roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!await _roleManager.RoleExistsAsync("Usuario")) await _roleManager.CreateAsync(new IdentityRole("Usuario"));

            if (await _roleManager.RoleExistsAsync(roleName)) return BadRequest($"El rol '{roleName}' ya existe.");

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded) return Ok($"Rol '{roleName}' creado exitosamente.");
            _logger.LogError($"Error al crear rol '{roleName}'.", new Exception(string.Join(", ", result.Errors.Select(e => e.Description))));
            return BadRequest(result.Errors);
        }

        [HttpPost("assignrole")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto assignRoleDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState); // Validar DTO
            var user = await _userManager.FindByEmailAsync(assignRoleDto.Email);
            if (user == null) return NotFound($"Usuario con email '{assignRoleDto.Email}' no encontrado.");
            if (!await _roleManager.RoleExistsAsync(assignRoleDto.RoleName)) return BadRequest($"Rol '{assignRoleDto.RoleName}' no existe.");

            // Evitar asignar el mismo rol múltiples veces (opcional, AddToRoleAsync lo maneja)
            // if (await _userManager.IsInRoleAsync(user, assignRoleDto.RoleName)) return BadRequest($"Usuario ya tiene el rol '{assignRoleDto.RoleName}'.");

            var result = await _userManager.AddToRoleAsync(user, assignRoleDto.RoleName);
            if (result.Succeeded) return Ok($"Rol '{assignRoleDto.RoleName}' asignado a '{assignRoleDto.Email}'.");
            _logger.LogError($"Error al asignar rol '{assignRoleDto.RoleName}' a '{assignRoleDto.Email}'.", new Exception(string.Join(", ", result.Errors.Select(e => e.Description))));
            return BadRequest(result.Errors);
        }

        [HttpPut("unlock/{email}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnlockUser(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound($"Usuario con email '{email}' no encontrado.");

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user); // Importante resetear el contador

            if (result.Succeeded)
            {
                _logger.LogInfo($"Usuario '{email}' desbloqueado por admin.");
                return Ok($"Usuario '{email}' desbloqueado.");
            }
            _logger.LogError($"Error al desbloquear usuario '{email}'.", new Exception(string.Join(", ", result.Errors.Select(e => e.Description))));
            return BadRequest("No se pudo desbloquear al usuario.");
        }

        // --- NUEVOS ENDPOINTS CRUD PARA USUARIOS ---

        // GET: api/Auth/users
        [HttpGet("users")]
        [Authorize(Roles = "Admin")] // Solo Admins pueden listar usuarios
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    Nombre = user.Nombre,
                    Apellido = user.Apellido,
                    Cedula = user.Cedula,
                    IsLockedOut = await _userManager.IsLockedOutAsync(user),
                    Roles = roles.ToList()
                });
            }
            return Ok(userDtos);
        }

        // GET: api/Auth/users/{email}
        [HttpGet("users/{email}")]
        [Authorize(Roles = "Admin")] // Solo Admins pueden ver detalles
        public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Usuario con email '{email}' no encontrado.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                Cedula = user.Cedula,
                IsLockedOut = await _userManager.IsLockedOutAsync(user),
                Roles = roles.ToList()
            };
            return Ok(userDto);
        }

        // PUT: api/Auth/users/{email}
        [HttpPut("users/{email}")]
        [Authorize(Roles = "Admin")] // Solo Admins pueden actualizar
        public async Task<IActionResult> UpdateUser(string email, [FromBody] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Usuario con email '{email}' no encontrado.");
            }

            // Actualizar los datos permitidos
            user.Nombre = updateUserDto.Nombre;
            user.Apellido = updateUserDto.Apellido;
            user.Cedula = updateUserDto.Cedula;
            // Podrías añadir lógica para actualizar el Email si es necesario,
            // pero requiere confirmación y manejo cuidadoso ya que es el UserName.

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInfo($"Usuario '{email}' actualizado por admin.");
                return NoContent(); // Éxito sin contenido
            }

            _logger.LogError($"Error al actualizar usuario '{email}'.", new Exception(string.Join(", ", result.Errors.Select(e => e.Description))));
            return BadRequest(result.Errors);
        }

        // DELETE: api/Auth/users/{email}
        [HttpDelete("users/{email}")]
        [Authorize(Roles = "Admin")] // Solo Admins pueden eliminar
        public async Task<IActionResult> DeleteUser(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Usuario con email '{email}' no encontrado.");
            }

            // Opcional: Impedir que el admin se borre a sí mismo
            // var currentUserEmail = User.FindFirstValue(ClaimTypes.Email);
            // if (user.Email.Equals(currentUserEmail, StringComparison.OrdinalIgnoreCase))
            // {
            //     return BadRequest("No puedes eliminar tu propia cuenta de administrador.");
            // }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInfo($"Usuario '{email}' eliminado por admin.");
                return NoContent(); // Éxito sin contenido
            }

            _logger.LogError($"Error al eliminar usuario '{email}'.", new Exception(string.Join(", ", result.Errors.Select(e => e.Description))));
            return BadRequest(result.Errors);
        }
    }

    // DTO auxiliar que ya tenías
    public class AssignRoleDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Email { get; set; } = "";
        [System.ComponentModel.DataAnnotations.Required]
        public string RoleName { get; set; } = "";
    }
}