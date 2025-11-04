using SalesManager.BusinessObjects.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.BusinessObjects.Interfaces
{
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        Task<(IReadOnlyList<Employee> Employees, int TotalCount)> FindEmployeesAsync(string searchTerm, int pageNumber, int pageSize);
    }
}
