using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Orders
{
    public class CreateOrderRequestDto
    {
        [Required(ErrorMessage = "El ID del cliente es obligatorio.")]
        public string CustomerID { get; set; } = "";

        [Required]
        [MinLength(1, ErrorMessage = "La orden debe tener al menos un producto.")]
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();

        // (Puedes añadir más campos como ShipAddress, ShipCity si es necesario)
    }
}
