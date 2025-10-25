using SalesManager.BusinessObjects.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.BusinessObjects.Interfaces
{
    // Hereda del genérico para obtener el CRUD básico
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        // Podrías añadir métodos específicos si los necesitaras,
        // como buscar categorías por nombre, etc.
        Task<(IReadOnlyList<Category> Categories, int TotalCount)> FindCategoriesAsync(string searchTerm, int pageNumber, int pageSize);
    }
}
