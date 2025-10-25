using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SalesManager.BusinessObjects.Entities; 
using SalesManager.UseCases.DTOs.Auth; 
using SalesManager.UseCases.Interfaces; 
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.Features
{
    public class AuthService : IAuthService 
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILoggerService _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILoggerService logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<TokenResponseDto> LoginAsync(UserLoginRequestDto loginRequest) //
        {
            var user = await _userManager.FindByEmailAsync(loginRequest.Email);
            if (user == null)
            {
                return new TokenResponseDto { IsSuccess = false, ErrorMessage = "Credenciales inválidas." }; //
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                return new TokenResponseDto { IsSuccess = false, ErrorMessage = "Cuenta bloqueada. Contacte al administrador." }; //
            }

            if (!result.Succeeded)
            {
                return new TokenResponseDto { IsSuccess = false, ErrorMessage = "Credenciales inválidas." }; //
            }

            _logger.LogInfo($"Usuario {loginRequest.Email} ha iniciado sesión.");
            return await GenerateJwtToken(user);
        }

        public async Task<TokenResponseDto> RegisterAsync(UserRegisterRequestDto registerRequest, string role) //
        {
            var userExists = await _userManager.FindByEmailAsync(registerRequest.Email);
            if (userExists != null)
            {
                return new TokenResponseDto { IsSuccess = false, ErrorMessage = "Ya existe un usuario con ese correo." }; //
            }

            var user = new ApplicationUser //
            {
                UserName = registerRequest.Email,
                Email = registerRequest.Email,
                Nombre = registerRequest.Nombre,
                Apellido = registerRequest.Apellido,
                Cedula = registerRequest.Cedula,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, registerRequest.Password);

            if (!result.Succeeded)
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError($"Error al crear usuario {user.Email}. Errores: {errors}", null);
                return new TokenResponseDto { IsSuccess = false, ErrorMessage = $"Error al crear el usuario: {errors}" }; //
            }

            await _userManager.AddToRoleAsync(user, role ?? "Usuario");

            _logger.LogInfo($"Nuevo usuario registrado: {user.Email}");
            return await GenerateJwtToken(user);
        }

        private async Task<TokenResponseDto> GenerateJwtToken(ApplicationUser user) //
        {
            var jwtSettings = _configuration.GetSection("Jwt"); //
            var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key no está configurada en appsettings.json"); //
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.Now.AddHours(Convert.ToDouble(jwtSettings["DurationInHours"] ?? "1"));
            var userEmail = user.Email ?? throw new InvalidOperationException("El usuario no tiene email configurado");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userEmail),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, userEmail)
            };

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );

            return new TokenResponseDto //
            {
                IsSuccess = true,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = expiration
            };
        }
    }
}