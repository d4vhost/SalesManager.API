using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Auth
{
    public class UserRegisterRequestDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        public string Apellido { get; set; } = "";

        [Required(ErrorMessage = "La cédula es obligatoria.")]
        public string Cedula { get; set; } = "";

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "La clave es obligatoria.")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Debe confirmar la clave.")]
        [Compare("Password", ErrorMessage = "Las claves no coinciden.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
