using FluentValidation;
using SalesManager.UseCases.DTOs.Orders;

namespace SalesManager.UseCases.Validation
{
    public class OrderItemValidator : AbstractValidator<OrderItemDto>
    {
        public OrderItemValidator()
        {
            RuleFor(x => x.ProductID)
                .GreaterThan(0).WithMessage("El ID del producto debe ser válido.");

            RuleFor(x => x.Quantity)
                .GreaterThan((short)0).WithMessage("La cantidad debe ser mayor que cero.");
            // El tipo es short, así que casteamos 0 a short
        }
    }
}