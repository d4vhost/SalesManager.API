using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Customers
{
    public class CustomerDto
    {
        public string CustomerID { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string? ContactName { get; set; }
        public string? Phone { get; set; }
    }
}