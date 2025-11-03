using SalesManager.BusinessObjects.Entities;
using SalesManager.BusinessObjects.Interfaces;
using SalesManager.UseCases.DTOs.Orders;
using SalesManager.UseCases.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.UseCases.Features
{
    public class CreateOrderInteractor
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _logger;
        private const decimal VatRate = 0.12m;

        public CreateOrderInteractor(IUnitOfWork unitOfWork, ILoggerService logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> HandleAsync(CreateOrderRequestDto request)
        {
            // --- 1. Validaciones de Negocio ---
            var duplicateProducts = request.Items
                .GroupBy(i => i.ProductID)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToList();

            if (duplicateProducts.Any())
            {
                throw new InvalidOperationException($"Productos duplicados en la orden: {string.Join(", ", duplicateProducts)}");
            }

            var newOrder = new Order
            {
                CustomerID = request.CustomerID,
                OrderDate = DateTime.UtcNow,
                ShipAddress = "N/A",
                ShipCity = "N/A",
                ShipCountry = "N/A",
                Freight = 0m
            };

            decimal subtotalOrder = 0m;

            // --- 2. Procesar cada línea de producto ---
            foreach (var item in request.Items)
            {
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(item.ProductID);

                if (product == null || product.UnitsInStock == null || product.UnitsInStock <= 0 || product.Discontinued)
                {
                    throw new InvalidOperationException($"Producto ID {item.ProductID} no existe, está descontinuado o no tiene stock.");
                }

                if (product.UnitsInStock < item.Quantity)
                {
                    throw new InvalidOperationException($"Stock insuficiente para '{product.ProductName}'. Solicitado: {item.Quantity}, Disponible: {product.UnitsInStock}.");
                }

                // --- 3. Descontar Stock ---
                product.UnitsInStock = (short)(product.UnitsInStock!.Value - item.Quantity);

                // ❌ ELIMINAR ESTA LÍNEA (causa el error)
                // _unitOfWork.ProductRepository.Update(product);

                // ✅ EF Core ya rastrea el cambio automáticamente

                var orderDetail = new OrderDetail
                {
                    ProductID = product.ProductID,
                    UnitPrice = product.UnitPrice ?? 0m,
                    Quantity = item.Quantity,
                    Discount = 0
                };
                newOrder.OrderDetails.Add(orderDetail);

                decimal lineSubtotal = (orderDetail.UnitPrice * orderDetail.Quantity * (1 - (decimal)orderDetail.Discount));
                subtotalOrder += lineSubtotal;
            }

            // --- 4. Calcular Totales ---
            newOrder.Subtotal = subtotalOrder;
            newOrder.VatAmount = Math.Round(subtotalOrder * VatRate, 2);
            newOrder.TotalAmount = newOrder.Subtotal + newOrder.VatAmount + (newOrder.Freight ?? 0m);

            // --- 5. Guardar la Orden ---
            await _unitOfWork.OrderRepository.AddAsync(newOrder);

            int changes = await _unitOfWork.SaveChangesAsync();

            if (changes > 0)
            {
                _logger.LogInfo($"Nueva orden creada con ID: {newOrder.OrderID}. Subtotal: {newOrder.Subtotal:N2}, IVA: {newOrder.VatAmount:N2}, Total: {newOrder.TotalAmount:N2}");
                return newOrder.OrderID;
            }
            else
            {
                _logger.LogError($"No se pudo guardar la orden para el cliente {request.CustomerID}. SaveChangesAsync devolvió 0.", null);
                throw new Exception("No se pudo guardar la orden. No se realizaron cambios en la base de datos.");
            }
        }
    }
}