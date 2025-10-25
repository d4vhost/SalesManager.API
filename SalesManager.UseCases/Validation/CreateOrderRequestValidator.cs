using FluentValidation;
using SalesManager.UseCases.DTOs.Orders;

namespace SalesManager.UseCases.Validation
{
    public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequestDto>
    {
        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.CustomerID)
                .NotEmpty().WithMessage("El ID del cliente es obligatorio.");
            // Podrías añadir validación de formato si el ID tiene uno específico

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("La orden debe contener al menos un producto.");

            // Valida cada item dentro de la lista Items usando otro validador
            RuleForEach(x => x.Items).SetValidator(new OrderItemValidator());
        }
    }
}