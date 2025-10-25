using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SalesManager.BusinessObjects.Entities;
using SalesManager.Repositories.Persistence;
using SalesManager.UseCases.Interfaces;
using System;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore; // <-- AÑADE ESTA LÍNEA (por si acaso y para DbUpdateConcurrencyException)


namespace SalesManager.Repositories.Services
{
    public class LoggerService : ILoggerService
    {
        private readonly ILogger<LoggerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoggerService(
            ILogger<LoggerService> logger,
            IServiceProvider serviceProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public void LogDebug(string message) => _logger.LogDebug(message);
        public void LogInfo(string message) => _logger.LogInformation(message);

        // Implementación actualizada para coincidir con la interfaz
        public void LogWarn(string message, DbUpdateConcurrencyException? ex = null)
        {
            // Loguea con el logger estándar
            _logger.LogWarning(ex, message); // Pasamos la excepción si existe
        }


        public void LogError(string message, Exception? ex = null)
        {
            _logger.LogError(ex, message);
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var httpContext = _httpContextAccessor.HttpContext;
                    var errorLog = new ErrorLog
                    {
                        Timestamp = DateTime.UtcNow,
                        LogLevel = "Error",
                        Message = message,
                        StackTrace = ex?.ToString(),
                        RequestPath = httpContext?.Request.Path,
                        HttpMethod = httpContext?.Request.Method,
                        UserName = httpContext?.User?.FindFirstValue(ClaimTypes.Email) ?? httpContext?.User?.Identity?.Name ?? "Anónimo"
                    };
                    dbContext.ErrorLogs.Add(errorLog);
                    dbContext.SaveChanges();
                }
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "¡¡¡FALLO AL GUARDAR ERROR EN LA BASE DE DATOS!!!");
            }
        }
    }
}