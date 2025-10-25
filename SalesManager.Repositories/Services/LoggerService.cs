using Microsoft.AspNetCore.Http; // Necesario para IHttpContextAccessor
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SalesManager.BusinessObjects.Entities; // Necesario para ErrorLog
using SalesManager.Repositories.Persistence; // Necesario para ApplicationDbContext
using SalesManager.UseCases.Interfaces;
using System;
using System.Security.Claims; // Necesario para ClaimTypes

namespace SalesManager.Repositories.Services
{
    public class LoggerService : ILoggerService
    {
        private readonly ILogger<LoggerService> _logger; // Logger estándar (opcional, puedes quitarlo si solo quieres BD)
        private readonly IServiceProvider _serviceProvider; // Para obtener DbContext en un Singleton
        private readonly IHttpContextAccessor _httpContextAccessor; // Para obtener datos del request

        // Inyectamos IServiceProvider y IHttpContextAccessor
        public LoggerService(
            ILogger<LoggerService> logger,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        // Los métodos LogInfo, LogWarn, LogDebug pueden seguir usando el logger estándar o ser eliminados/modificados
        public void LogDebug(string message) => _logger.LogDebug(message);
        public void LogInfo(string message) => _logger.LogInformation(message);
        public void LogWarn(string message) => _logger.LogWarning(message);

        // --- MÉTODO MODIFICADO ---
        public void LogError(string message, Exception? ex = null)
        {
            // 1. Loguear con el logger estándar (opcional)
            _logger.LogError(ex, message);

            // 2. Intentar guardar en la Base de Datos
            try
            {
                // **Importante:** Como LoggerService está registrado como Singleton
                // y DbContext es Scoped, NO podemos inyectar DbContext directamente en el constructor.
                // En su lugar, obtenemos una instancia Scoped de DbContext aquí usando IServiceProvider.
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var httpContext = _httpContextAccessor.HttpContext;

                    var errorLog = new ErrorLog
                    {
                        Timestamp = DateTime.UtcNow,
                        LogLevel = "Error",
                        Message = message,
                        StackTrace = ex?.ToString(), // Guarda el stack trace completo si hay excepción
                        // Obtener datos del contexto HTTP si está disponible
                        RequestPath = httpContext?.Request.Path,
                        HttpMethod = httpContext?.Request.Method,
                        // Obtener el email del usuario autenticado desde los claims del token JWT
                        UserName = httpContext?.User?.FindFirstValue(ClaimTypes.Email) ?? httpContext?.User?.Identity?.Name ?? "Anónimo"
                    };

                    dbContext.ErrorLogs.Add(errorLog);
                    dbContext.SaveChanges(); // Guardamos inmediatamente el error
                }
            }
            catch (Exception dbEx)
            {
                // Si falla al guardar en la BD, loguear ese error con el logger estándar
                _logger.LogError(dbEx, "¡¡¡FALLO AL GUARDAR ERROR EN LA BASE DE DATOS!!!");
            }
        }
    }
}