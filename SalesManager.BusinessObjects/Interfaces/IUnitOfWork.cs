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

        // Método para guardar todos los cambios en la base de datos
        Task<int> SaveChangesAsync();
    }
}
