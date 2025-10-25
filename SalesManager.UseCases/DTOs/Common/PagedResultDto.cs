using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.DTOs.Common
{
    // Un DTO genérico para devolver listas paginadas
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new List<T>(); // La lista de items de la página actual
        public int PageNumber { get; set; }     // El número de la página actual
        public int PageSize { get; set; }       // Cuántos items hay por página
        public int TotalCount { get; set; }     // Cuántos items hay en total (sin paginar)
        public int TotalPages { get; set; }     // Cuántas páginas hay en total

        // Propiedad calculada para saber si hay una página anterior
        public bool HasPreviousPage => PageNumber > 1;
        // Propiedad calculada para saber si hay una página siguiente
        public bool HasNextPage => PageNumber < TotalPages;

        // Constructor para facilitar la creación
        public PagedResultDto(List<T> items, int pageNumber, int pageSize, int totalCount)
        {
            Items = items;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize); // Calcula el total de páginas
        }
    }
}
