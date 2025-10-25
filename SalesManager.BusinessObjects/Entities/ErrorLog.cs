using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.BusinessObjects.Entities
{
    public class ErrorLog
    {
        [Key] // Marca Id como clave primaria
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Fecha y hora del error (en UTC)

        [MaxLength(100)] // Limita la longitud en la BD
        public string? LogLevel { get; set; } // Nivel del log (Error, Warning, etc.)

        [Required] // El mensaje es obligatorio
        public string Message { get; set; } = string.Empty; // Mensaje del error

        public string? StackTrace { get; set; } // Pila de llamadas (stack trace)

        // Información de contexto (requerida por el PDF.pdf"])
        public string? RequestPath { get; set; } // Endpoint o pantalla donde ocurrió
        public string? HttpMethod { get; set; } // Método HTTP (GET, POST, etc.)
        public string? UserName { get; set; } // Usuario autenticado (si existe)
    }
}
