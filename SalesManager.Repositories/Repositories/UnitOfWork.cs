using SalesManager.Repositories.Persistence;
using SalesManager.BusinessObjects.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading.Tasks; 

namespace SalesManager.Repositories.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        // Propiedades para acceder a los repositorios
        public IProductRepository ProductRepository { get; }
        public ICustomerRepository CustomerRepository { get; }
        public IOrderRepository OrderRepository { get; }
        public ICategoryRepository CategoryRepository { get; }
        public ISupplierRepository SupplierRepository { get; }
        public IEmployeeRepository EmployeeRepository { get; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            // Inicializar todos los repositorios
            ProductRepository = new ProductRepository(_context);
            CustomerRepository = new CustomerRepository(_context);
            OrderRepository = new OrderRepository(_context);
            CategoryRepository = new CategoryRepository(_context);
            SupplierRepository = new SupplierRepository(_context);
            EmployeeRepository = new EmployeeRepository(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}