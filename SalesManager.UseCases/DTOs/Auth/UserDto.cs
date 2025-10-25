using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Auth
{
    public class UserDto
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string Cedula { get; set; } = "";
        public bool IsLockedOut { get; set; } // Para saber si está bloqueado
        public List<string> Roles { get; set; } = new List<string>(); // Roles asignados
    }
}
