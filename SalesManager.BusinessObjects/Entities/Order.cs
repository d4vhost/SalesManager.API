using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalesManager.BusinessObjects.Entities
{
    public class Order
    {
        public int OrderID { get; set; }
        public string? CustomerID { get; set; }
        public int? EmployeeID { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? RequiredDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public int? ShipVia { get; set; }
        public decimal? Freight { get; set; }
        public string? ShipName { get; set; }
        public string? ShipAddress { get; set; }
        public string? ShipCity { get; set; }
        public string? ShipRegion { get; set; }
        public string? ShipPostalCode { get; set; }
        public string? ShipCountry { get; set; }

        [Column(TypeName = "decimal(18, 2)")] // Especificar precisión para SQL Server
        public decimal Subtotal { get; set; } = 0m;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal VatAmount { get; set; } = 0m; // Monto del IVA

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; } = 0m; // Total = Subtotal + VatAmount + Freight

        // Propiedades de navegación
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
