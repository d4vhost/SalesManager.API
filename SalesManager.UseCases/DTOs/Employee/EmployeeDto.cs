using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Employee
{
    public class EmployeeDto
    {
        public int EmployeeID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Email { get; set; } // Email de la cuenta de usuario
        public string? Phone { get; set; }
        public bool IsLockedOut { get; set; } // Para el botón de desbloqueo
    }
}
