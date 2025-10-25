using SalesManager.BusinessObjects.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.BusinessObjects.Interfaces
{
    public interface ISupplierRepository : IGenericRepository<Supplier>
    {
        // Método para búsqueda y paginación
        Task<(IReadOnlyList<Supplier> Suppliers, int TotalCount)> FindSuppliersAsync(string searchTerm, int pageNumber, int pageSize);
    }
}
