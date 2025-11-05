using FluentValidation;
using SalesManager.UseCases.DTOs.Auth;
using System.Linq; // Necesario para .All() y char.IsLetter
using System.Text.RegularExpressions; // Para validaciones con Regex

namespace SalesManager.UseCases.Validation
{
    public class UserRegisterRequestValidator : AbstractValidator<UserRegisterRequestDto>
    {
        public UserRegisterRequestValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(50).WithMessage("El nombre no puede exceder los 50 caracteres.")
                .Must(BeOnlyLetters).WithMessage("El nombre solo puede contener letras y espacios."); // Requisito 6: solo letras

            RuleFor(x => x.Apellido)
                .NotEmpty().WithMessage("El apellido es obligatorio.")
                .MaximumLength(50).WithMessage("El apellido no puede exceder los 50 caracteres.")
                .Must(BeOnlyLetters).WithMessage("El apellido solo puede contener letras y espacios."); // Requisito 6: solo letras

            RuleFor(x => x.Cedula)
                .NotEmpty().WithMessage("La cédula es obligatoria.")
                .Length(10).WithMessage("La cédula debe tener 10 dígitos.")
                .Must(BeNumeric).WithMessage("La cédula solo debe contener números.") // Requisito 6: solo números
                .Must(BeValidEcuadorianCedula).WithMessage("La cédula ingresada no es válida."); // Requisito 6: validar cédula

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo es obligatorio.")
                .EmailAddress().WithMessage("El formato del correo no es válido."); // Requisito 6: formato email

            // --- INICIO DE MODIFICACIÓN: Relajar reglas de contraseña ---
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La clave es obligatoria.")
                .MinimumLength(4).WithMessage("La clave debe tener al menos 4 caracteres.");
            // .MaximumLength(10).WithMessage("La clave no puede exceder los 10 caracteres.") 
            // .Matches("[A-Z]").WithMessage("La clave debe contener al menos una mayúscula.") 
            // .Matches("[a-z]").WithMessage("La clave debe contener al menos una minúscula.") 
            // .Matches("[0-9]").WithMessage("La clave debe contener al menos un número.") 
            // .Matches("[^a-zA-Z0-9]").WithMessage("La clave debe contener al menos un caracter especial.");
            // --- FIN DE MODIFICACIÓN ---

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Las claves no coinciden.");
        }

        // --- Funciones de Validación Personalizadas ---

        private bool BeOnlyLetters(string name)
        {
            // Permite letras y espacios (puedes ajustar si necesitas tildes, ñ, etc.)
            return !string.IsNullOrWhiteSpace(name) && name.All(c => char.IsLetter(c) || char.IsWhiteSpace(c));
        }

        private bool BeNumeric(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.All(char.IsDigit);
        }

        // Algoritmo de validación de cédula ecuatoriana (simplificado)
        private bool BeValidEcuadorianCedula(string cedula)
        {
            if (cedula == null || cedula.Length != 10 || !cedula.All(char.IsDigit))
                return false;

            int provincia = int.Parse(cedula.Substring(0, 2));
            if (provincia < 1 || provincia > 24) // Validación básica de provincia
                return false;

            int tercerDigito = int.Parse(cedula[2].ToString());
            if (tercerDigito < 0 || tercerDigito > 5) // Validación básica del tercer dígito
                return false;

            int[] coeficientes = { 2, 1, 2, 1, 2, 1, 2, 1, 2 };
            int suma = 0;
            for (int i = 0; i < 9; i++)
            {
                int producto = int.Parse(cedula[i].ToString()) * coeficientes[i];
                suma += (producto >= 10) ? producto - 9 : producto;
            }

            int residuo = suma % 10;
            int digitoVerificadorCalculado = (residuo == 0) ? 0 : 10 - residuo;
            int digitoVerificadorReal = int.Parse(cedula[9].ToString());

            return digitoVerificadorCalculado == digitoVerificadorReal;
        }
    }
}