using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SalesManager.BusinessObjects.Entities;
using SalesManager.UseCases.DTOs.Auth;
using SalesManager.UseCases.Interfaces;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System; // Added for Exception

namespace SalesManager.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILoggerService _logger;

        public AuthController(
            IAuthService authService,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILoggerService logger)
        {
            _authService = authService;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // --- Authentication Endpoints (Login, Register) ---

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
        [AllowAnonymous] // Or [Authorize(Roles = "Admin")]  
        public async Task<IActionResult> Register([FromBody] UserRegisterRequestDto registerRequest)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.RegisterAsync(registerRequest, "Usuario");
            if (!result.IsSuccess) return BadRequest(new { message = result.ErrorMessage });
            return Ok(new { message = "User registered successfully." });
        }

        // --- Role Management Endpoints ---

        // GET: api/Auth/roles
        /// <summary>
        /// Gets a list of all available role names.
        /// </summary>
        [HttpGet("roles")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<string>>> GetRoles()
        {
            var roles = await _roleManager.Roles
                .Select(r => r.Name)
                .Where(r => r != null) // Ensure no null names
                .ToListAsync();
            return Ok(roles);
        }

        [HttpPost("roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRole([FromBody] string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return BadRequest("Role name is required.");
            // Ensure base roles exist
            if (!await _roleManager.RoleExistsAsync("Admin")) await _roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!await _roleManager.RoleExistsAsync("Usuario")) await _roleManager.CreateAsync(new IdentityRole("Usuario"));

            if (await _roleManager.RoleExistsAsync(roleName)) return BadRequest($"Role '{roleName}' already exists.");

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded) return Ok($"Role '{roleName}' created successfully.");
            _logger.LogError($"Error creating role '{roleName}'.", new Exception(string.Join(", ", result.Errors.Select(e => e.Description))));
            return BadRequest(result.Errors);
        }

        // --- DEPRECATED: We now use set-roles ---
        // [HttpPost("assignrole")]
        // ...

        [HttpPut("unlock/{email}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnlockUser(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound($"User with email '{email}' not found.");

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user); // Important to reset the counter

            if (result.Succeeded)
            {
                _logger.LogInfo($"User '{email}' unlocked by admin.");
                return Ok($"User '{email}' unlocked.");
            }
            _logger.LogError($"Error unlocking user '{email}'.", new Exception(string.Join(", ", result.Errors.Select(e => e.Description))));
            return BadRequest("Could not unlock user.");
        }

        // --- USER ACCOUNT CRUD ---

        // GET: api/Auth/users
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]   // Only Admins can list users
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
        [Authorize(Roles = "Admin")]   // Only Admins can view details
        public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"User with email '{email}' not found.");
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

        // PUT: api/Auth/users/{email} (This updates data like Name, Cedula)
        [HttpPut("users/{email}")]
        [Authorize(Roles = "Admin")]   // Only Admins can update
        public async Task<IActionResult> UpdateUser(string email, [FromBody] UpdateUserDto updateUserDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"User with email '{email}' not found.");
            }

            // Update allowed data
            user.Nombre = updateUserDto.Nombre;
            user.Apellido = updateUserDto.Apellido;
            user.Cedula = updateUserDto.Cedula;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInfo($"User '{email}' updated by admin.");
                return NoContent(); // Success without content
            }

            _logger.LogError($"Error updating user '{email}'.", new Exception(string.Join(", ", result.Errors.Select(e => e.Description))));
            return BadRequest(result.Errors);
        }

        // DELETE: api/Auth/users/{email}
        [HttpDelete("users/{email}")]
        [Authorize(Roles = "Admin")]   // Only Admins can delete
        public async Task<IActionResult> DeleteUser(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"User with email '{email}' not found.");
            }

            // (This logic is now in EmployeeService, but we leave a
            // direct user delete here in case a user is not an employee)
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInfo($"User '{email}' deleted by admin.");
                return NoContent(); // Success without content
            }

            _logger.LogError($"Error deleting user '{email}'.", new Exception(string.Join(", ", result.Errors.Select(e => e.Description))));
            return BadRequest(result.Errors);
        }

        // --- START OF NEW ADMIN ACTIONS ---

        /// <summary>
        /// Replaces all roles for a user with the provided list.
        /// </summary>
        [HttpPut("users/{email}/set-roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetUserRoles(string email, [FromBody] AdminSetRolesDto dto)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound($"User '{email}' not found.");

            // Validate all roles exist
            foreach (var roleName in dto.Roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    return BadRequest($"Role '{roleName}' does not exist.");
                }
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                return BadRequest(new { message = "Failed to remove current roles.", errors = removeResult.Errors });
            }

            var addResult = await _userManager.AddToRolesAsync(user, dto.Roles);
            if (!addResult.Succeeded)
            {
                return BadRequest(new { message = "Failed to add new roles.", errors = addResult.Errors });
            }

            return Ok(new { message = $"Roles for '{email}' updated successfully." });
        }


        /// <summary>
        /// Allows an admin to force a password change for a user.
        /// </summary>
        [HttpPut("users/{email}/admin-change-password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminChangePassword(string email, [FromBody] AdminChangePasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound($"User '{email}' not found.");

            // --- START OF FIX ---
            // We must generate a reset token and use it to apply the new password.
            // This is the correct admin flow and bypasses the "old password" requirement.

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

            if (!resetResult.Succeeded)
            {
                _logger.LogError($"Failed to reset password for '{email}'.", new Exception(string.Join(", ", resetResult.Errors.Select(e => e.Description))));
                // This will fail if the new password doesn't meet Identity complexity rules (Req 18)
                return BadRequest(new { message = "Failed to set new password. Ensure it meets complexity requirements.", errors = resetResult.Errors });
            }
            // --- END OF FIX ---

            /* --- OLD, FLAWED LOGIC ---
            // Force change: Remove old password, add new one
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                _logger.LogWarn($"Could not remove old password for '{email}'. Proceeding to add new one.", null);
            }

            var addResult = await _userManager.AddPasswordAsync(user, dto.NewPassword);
            if (!addResult.Succeeded)
            {
                _logger.LogError($"Failed to add new password for '{email}'.", new Exception(string.Join(", ", addResult.Errors.Select(e => e.Description))));
                return BadRequest(new { message = "Failed to set new password.", errors = addResult.Errors });
            }
            */

            _logger.LogInfo($"Password for '{email}' changed by admin.");
            return Ok(new { message = $"Password for '{email}' changed successfully." });
        }

        /// <summary>
        /// Allows an admin to force an email and username change.
        /// </summary>
        [HttpPut("users/{email}/admin-change-email")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminChangeEmail(string email, [FromBody] AdminChangeEmailDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound($"User '{email}' not found.");

            // Check if new email is already taken
            var existingUser = await _userManager.FindByEmailAsync(dto.NewEmail);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return BadRequest(new { message = $"The email '{dto.NewEmail}' is already taken." });
            }

            // Change Email
            var emailResult = await _userManager.SetEmailAsync(user, dto.NewEmail);
            if (!emailResult.Succeeded)
            {
                return BadRequest(new { message = "Failed to set new email.", errors = emailResult.Errors });
            }

            // Also change UserName (which Identity uses for login)
            var userResult = await _userManager.SetUserNameAsync(user, dto.NewEmail);
            if (!userResult.Succeeded)
            {
                return BadRequest(new { message = "Failed to set new username.", errors = userResult.Errors });
            }

            // (Optional) Mark new email as confirmed
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            _logger.LogInfo($"Email for user '{email}' changed to '{dto.NewEmail}' by admin.");
            return Ok(new { message = $"User's email changed to '{dto.NewEmail}' successfully." });
        }

        // --- END OF NEW ADMIN ACTIONS ---
    }
}