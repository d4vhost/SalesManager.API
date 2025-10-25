using SalesManager.BusinessObjects.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.BusinessObjects.Interfaces
{
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        // Aquí irán métodos de búsqueda inteligente
        // Task<IReadOnlyList<Customer>> FindCustomersAsync(string searchTerm);
    }
}
