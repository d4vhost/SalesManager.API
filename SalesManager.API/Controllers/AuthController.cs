using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SalesManager.BusinessObjects.Entities;
using SalesManager.UseCases.DTOs.Auth;
using SalesManager.UseCases.Interfaces;
using System.Threading.Tasks;

namespace SalesManager.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; // Para gestionar roles

        public AuthController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _authService = authService;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // POST: api/Auth/Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto loginRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(loginRequest);

            if (!result.IsSuccess)
            {
                // Devolvemos Unauthorized si las credenciales son inválidas o la cuenta está bloqueada
                return Unauthorized(new { message = result.ErrorMessage });
            }

            return Ok(result);
        }

        // POST: api/Auth/Register
        [HttpPost("register")]
        // Podrías restringir quién puede registrarse, por ejemplo, solo Admins
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequestDto registerRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Requisito 39: Por defecto, rol Usuarios.pdf"]
            // Si quieres permitir registro de Admins, necesitarás lógica adicional
            // o un endpoint separado [Authorize(Roles = "Admin")]
            var result = await _authService.RegisterAsync(registerRequest, "Usuario");

            if (!result.IsSuccess)
            {
                // Devolvemos BadRequest si el correo ya existe o hubo error de Identity
                return BadRequest(new { message = result.ErrorMessage });
            }

            // Podrías devolver Ok(result) si quieres retornar el token inmediatamente
            // o CreatedAtAction si tienes un endpoint para obtener info del usuario
            return Ok(new { message = "Usuario registrado exitosamente." });
        }

        // --- Gestión de Usuarios y Roles (Ejemplos, requiere [Authorize(Roles = "Admin")]) ---

        // POST: api/Auth/CreateRole
        [HttpPost("roles")]
        [Authorize(Roles = "Admin")] // Solo Admins pueden crear roles
        public async Task<IActionResult> CreateRole([FromBody] string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return BadRequest("El nombre del rol es requerido.");
            }

            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (roleExists)
            {
                return BadRequest($"El rol '{roleName}' ya existe.");
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded)
            {
                return Ok($"Rol '{roleName}' creado exitosamente.");
            }
            return BadRequest(result.Errors);
        }

        // POST: api/Auth/AssignRole
        [HttpPost("assignrole")]
        [Authorize(Roles = "Admin")] // Solo Admins pueden asignar roles
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto assignRoleDto)
        {
            var user = await _userManager.FindByEmailAsync(assignRoleDto.Email);
            if (user == null)
            {
                return NotFound($"Usuario con email '{assignRoleDto.Email}' no encontrado.");
            }

            var roleExists = await _roleManager.RoleExistsAsync(assignRoleDto.RoleName);
            if (!roleExists)
            {
                return BadRequest($"Rol '{assignRoleDto.RoleName}' no existe.");
            }

            var result = await _userManager.AddToRoleAsync(user, assignRoleDto.RoleName);
            if (result.Succeeded)
            {
                return Ok($"Rol '{assignRoleDto.RoleName}' asignado a '{assignRoleDto.Email}'.");
            }
            return BadRequest(result.Errors);
        }

        // PUT: api/Auth/UnlockUser/{email}
        // Requisito 25: Desbloquear usuario.pdf"]
        [HttpPut("unlock/{email}")]
        [Authorize(Roles = "Admin")] // Solo Admins pueden desbloquear
        public async Task<IActionResult> UnlockUser(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Usuario con email '{email}' no encontrado.");
            }

            // Resetea el contador de intentos fallidos y la fecha de bloqueo
            var result = await _userManager.SetLockoutEndDateAsync(user, null); // null o DateTimeOffset.MinValue
            await _userManager.ResetAccessFailedCountAsync(user);


            if (result.Succeeded)
            {
                return Ok($"Usuario '{email}' desbloqueado.");
            }
            return BadRequest("No se pudo desbloquear al usuario.");
        }
    }

    // DTO auxiliar para asignar roles
    public class AssignRoleDto
    {
        public string Email { get; set; }
        public string RoleName { get; set; }
    }
}