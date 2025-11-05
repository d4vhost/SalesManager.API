using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Employee
{
    public class CreateEmployeeRequestDto
    {
        [Required]
        [StringLength(25)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(25)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(10, MinimumLength = 10)]
        public string Cedula { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Title { get; set; }

        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        // --- INICIO DE MODIFICACIÓN: Requisito 18 (PDF) ---
        [StringLength(10, MinimumLength = 4, ErrorMessage = "La clave debe tener entre 4 y 10 caracteres.")]
        // --- FIN DE MODIFICACIÓN ---
        public string Password { get; set; } = string.Empty;
    }
}