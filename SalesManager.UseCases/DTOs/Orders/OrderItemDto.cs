using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Orders
{
    public class OrderItemDto
    {
        [Required]
        public int ProductID { get; set; }

        [Required]
        [Range(1, short.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1.")]
        public short Quantity { get; set; }
    }
}