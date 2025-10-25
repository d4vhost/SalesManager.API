using SalesManager.BusinessObjects.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesManager.UseCases.Interfaces
{
    public interface IPdfGeneratorService
    {
        // Método que toma una orden y devuelve el PDF como array de bytes
        Task<byte[]> GenerateInvoicePdfAsync(Order order);
    }
}
