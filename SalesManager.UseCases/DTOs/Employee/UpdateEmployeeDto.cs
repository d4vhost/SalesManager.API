using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Employee
{
    public class UpdateEmployeeDto
    {
        [Required]
        [StringLength(25)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(25)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Title { get; set; }

        // La cédula y el email no se deberían cambiar
        // public string Cedula { get; set; } = string.Empty;

        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
    }
}
