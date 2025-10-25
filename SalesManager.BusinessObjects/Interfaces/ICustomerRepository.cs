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
        Task<(IReadOnlyList<Customer> Customers, int TotalCount)> FindCustomersAsync(string searchTerm, int pageNumber, int pageSize);
    }
}
