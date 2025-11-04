using Microsoft.EntityFrameworkCore.Storage; // <-- ASEGÚRATE DE TENER ESTE USING
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.BusinessObjects.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // Aquí expones los repositorios que deben participar en la transacción
        IProductRepository ProductRepository { get; }
        ICustomerRepository CustomerRepository { get; }
        IOrderRepository OrderRepository { get; }
        ICategoryRepository CategoryRepository { get; }
        ISupplierRepository SupplierRepository { get; }
        IEmployeeRepository EmployeeRepository { get; }

        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}