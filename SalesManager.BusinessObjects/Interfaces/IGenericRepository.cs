using System.Collections.Generic;
using System.Threading.Tasks;

namespace SalesManager.BusinessObjects.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id); // <-- Añade '?'
        Task<T?> GetByIdAsync(string id); // <-- Añade '?'
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}