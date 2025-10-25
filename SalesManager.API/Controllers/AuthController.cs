using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SalesManager.BusinessObjects.Entities; // Necesario para ApplicationUser
using SalesManager.UseCases.DTOs.Auth; // Necesario para los DTOs de Auth
using SalesManager.UseCases.Interfaces; // Necesario para IAuthService
using System.Threading.Tasks; // Necesario para Task<>

namespace SalesManager.WebAPI.Controllers // Asegúrate que el namespace sea correcto
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager; // Para gestionar roles

        // Inyección de dependencias
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
        [AllowAnonymous] // Permite acceso sin autenticación
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto loginRequest)
        {
            // Valida el DTO recibido
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Llama al servicio de autenticación
            var result = await _authService.LoginAsync(loginRequest);

            if (!result.IsSuccess)
            {
                // Devuelve 401 Unauthorized si las credenciales son inválidas o la cuenta está bloqueada.pdf"]
                return Unauthorized(new { message = result.ErrorMessage });
            }

            // Devuelve 200 OK con el token JWT si el login es exitoso.pdf"]
            return Ok(result);
        }

        // POST: api/Auth/Register
        [HttpPost("register")]
        [AllowAnonymous] // O [Authorize(Roles = "Admin")] si solo admins pueden registrar
        public async Task<IActionResult> Register([FromBody] UserRegisterRequestDto registerRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Registra al usuario con el rol por defecto "Usuario".pdf"]
            var result = await _authService.RegisterAsync(registerRequest, "Usuario");

            if (!result.IsSuccess)
            {
                // Devuelve 400 Bad Request si el correo ya existe o hubo error de Identity
                return BadRequest(new { message = result.ErrorMessage });
            }

            // Devuelve 200 OK (o 201 Created si devuelves la info del usuario creado)
            return Ok(new { message = "Usuario registrado exitosamente." });
        }

        // --- Gestión de Roles (Ejemplo) ---

        // POST: api/Auth/roles
        [HttpPost("roles")]
        [Authorize(Roles = "Admin")] // Solo Admins pueden crear roles.pdf"]
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
                // Asegúrate de que los roles base existan (Admin, Usuario) la primera vez
                if (!await _roleManager.RoleExistsAsync("Admin")) await _roleManager.CreateAsync(new IdentityRole("Admin"));
                if (!await _roleManager.RoleExistsAsync("Usuario")) await _roleManager.CreateAsync(new IdentityRole("Usuario"));

                return Ok($"Rol '{roleName}' creado exitosamente.");
            }
            return BadRequest(result.Errors);
        }

        // POST: api/Auth/assignrole
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

        // PUT: api/Auth/unlock/{email}
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

            // Resetea la fecha de bloqueo y el contador de intentos fallidos
            var result = await _userManager.SetLockoutEndDateAsync(user, null); // Poner null quita el bloqueo
            await _userManager.ResetAccessFailedCountAsync(user);


            if (result.Succeeded)
            {
                return Ok($"Usuario '{email}' desbloqueado.");
            }
            return BadRequest("No se pudo desbloquear al usuario.");
        }
    }

    // DTO auxiliar para el body de AssignRole
    public class AssignRoleDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Email { get; set; } = "";
        [System.ComponentModel.DataAnnotations.Required]
        public string RoleName { get; set; } = "";
    }
}