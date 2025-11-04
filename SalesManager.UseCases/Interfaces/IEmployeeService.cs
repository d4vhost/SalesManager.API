using SalesManager.UseCases.DTOs.Employee;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.Interfaces
{
    public interface IEmployeeService
    {
        Task<int> CreateEmployeeAndUserAsync(CreateEmployeeRequestDto employeeDto);
        Task UpdateEmployeeAsync(int employeeId, UpdateEmployeeDto employeeDto);
        Task DeleteEmployeeAndUserAsync(int employeeId);
    }
}
