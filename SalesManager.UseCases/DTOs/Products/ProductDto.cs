using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Products
{
    public class ProductDto
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public decimal? UnitPrice { get; set; }
        public short? UnitsInStock { get; set; } // <-- Debe ser short?, no int?
    }
}