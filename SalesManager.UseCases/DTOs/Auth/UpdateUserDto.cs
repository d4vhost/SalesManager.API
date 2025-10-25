using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Auth
{
    // DTO para recibir datos al actualizar un usuario (sin contraseña)
    public class UpdateUserDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        public string Apellido { get; set; } = "";

        [Required(ErrorMessage = "La cédula es obligatoria.")]
        public string Cedula { get; set; } = "";

        // Nota: El Email (que es el UserName) y la Contraseña
        // generalmente se manejan con endpoints/procesos separados
        // por seguridad y complejidad (ej. cambio de contraseña, confirmación de email).
    }
}
