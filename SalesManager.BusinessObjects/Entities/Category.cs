using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.BusinessObjects.Entities
{
    public class Category
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = "";
        public string? Description { get; set; }
        public byte[]? Picture { get; set; }

        // Propiedad de navegación
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
