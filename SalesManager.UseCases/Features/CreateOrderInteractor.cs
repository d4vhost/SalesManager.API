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
    // Este Interactor implementa la lógica principal del Punto de Venta
    public class CreateOrderInteractor
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _logger;

        public CreateOrderInteractor(IUnitOfWork unitOfWork, ILoggerService logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> HandleAsync(CreateOrderRequestDto request)
        {
            // --- 1. Validaciones de Negocio ---

            // Requisito 3: "Un producto debería aparecer una sola vez en la orden."
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
                OrderDate = DateTime.Now,
                // (Mapear dirección de envío si existe en el DTO)
                ShipAddress = "N/A",
                ShipCity = "N/A",
                ShipCountry = "N/A"
            };

            decimal totalOrder = 0;

            // --- 2. Procesar cada línea de producto ---
            foreach (var item in request.Items)
            {
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(item.ProductID);

                // Requisito 8: "productos... con stock mayor que cero" (Aunque el repo ya lo filtra, validamos)
                if (product == null || product.UnitsInStock <= 0)
                {
                    throw new InvalidOperationException($"Producto ID {item.ProductID} no existe o no tiene stock.");
                }

                // Requisito 9: "no permitir vender más de lo que se tiene en stock"
                if (product.UnitsInStock < item.Quantity)
                {
                    throw new InvalidOperationException($"Stock insuficiente para {product.ProductName}. Solicitado: {item.Quantity}, Disponible: {product.UnitsInStock}.");
                }

                // --- 3. Descontar Stock y Calcular Totales ---

                // Requisito 7: "disminución AUTOMÁTICA del inventario"
                // Convertir explícitamente a short para evitar error de nulabilidad
                product.UnitsInStock = (short)(product.UnitsInStock!.Value - item.Quantity);
                _unitOfWork.ProductRepository.Update(product);

                var orderDetail = new OrderDetail
                {
                    ProductID = product.ProductID,
                    UnitPrice = product.UnitPrice ?? 0,
                    Quantity = item.Quantity,
                    Discount = 0 // (Manejar descuentos si es necesario)
                };

                newOrder.OrderDetails.Add(orderDetail);

                // Requisito 4: "Calcular totales, subtotales e IVA"
                totalOrder += (orderDetail.UnitPrice * orderDetail.Quantity);
            }

            newOrder.Freight = 0; // (Calcular costo de envío si aplica)
            // (Aquí aplicarías IVA si es necesario. Ej: totalOrder *= 1.12m;)

            // --- 4. Guardar la Orden (Transaccional) ---
            await _unitOfWork.OrderRepository.AddAsync(newOrder);

            // Requisito 17: "Utilizar transacciones". SaveChangesAsync del UnitOfWork
            // guardará la Orden, los OrderDetails Y la actualización del Stock
            // en una sola transacción. Si algo falla, todo se revierte.
            int changes = await _unitOfWork.SaveChangesAsync();

            if (changes > 0)
            {
                _logger.LogInfo($"Nueva orden creada con ID: {newOrder.OrderID}");
                return newOrder.OrderID; // Devuelve el ID de la nueva orden
            }

            throw new Exception("No se pudo guardar la orden.");
        }
    }
}