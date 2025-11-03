using SalesManager.BusinessObjects.Entities; // Necesario para Order, OrderDetail, Product
using SalesManager.BusinessObjects.Interfaces; // Necesario para IUnitOfWork
using SalesManager.UseCases.DTOs.Orders; // Necesario para CreateOrderRequestDto, OrderItemDto
using SalesManager.UseCases.Interfaces; // Necesario para ILoggerService
using System;
using System.Collections.Generic; // Necesario para List<>
using System.Linq; // Necesario para GroupBy, Where, Select, Any, Sum
using System.Threading.Tasks; // Necesario para Task, async, await

namespace SalesManager.UseCases.Features
{
    // Este Interactor implementa la lógica principal del Punto de Venta
    public class CreateOrderInteractor
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _logger;

        // Define la tasa de IVA (ej. 12%) - Puedes moverla a appsettings.json si es configurable
        private const decimal VatRate = 0.12m;

        public CreateOrderInteractor(IUnitOfWork unitOfWork, ILoggerService logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> HandleAsync(CreateOrderRequestDto request)
        {
            // --- 1. Validaciones de Negocio ---

            // Requisito 3: "Un producto debería aparecer una sola vez en la orden.".pdf"]
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
                OrderDate = DateTime.UtcNow, // Usar UTC para fechas en servidor
                // (Mapear dirección de envío si existe en el DTO o desde el Customer)
                ShipAddress = "N/A",
                ShipCity = "N/A",
                ShipCountry = "N/A",
                Freight = 0m // Asigna valor inicial a Freight
            };

            decimal subtotalOrder = 0m;

            // --- 2. Procesar cada línea de producto ---
            foreach (var item in request.Items)
            {
                // GetByIdAsync usa FindAsync, por lo que 'product' es rastreado por EF Core
                var product = await _unitOfWork.ProductRepository.GetByIdAsync(item.ProductID);

                // Validación robusta de producto y stock
                // Requisito 8.pdf"]
                if (product == null || product.UnitsInStock == null || product.UnitsInStock <= 0 || product.Discontinued)
                {
                    throw new InvalidOperationException($"Producto ID {item.ProductID} no existe, está descontinuado o no tiene stock.");
                }

                // Requisito 9: Validar stock disponible.pdf"]
                if (product.UnitsInStock < item.Quantity)
                {
                    throw new InvalidOperationException($"Stock insuficiente para '{product.ProductName}'. Solicitado: {item.Quantity}, Disponible: {product.UnitsInStock}.");
                }

                // --- 3. Descontar Stock y Crear Detalle ---

                // Requisito 7: Disminución automática del inventario.pdf"]
                product.UnitsInStock = (short)(product.UnitsInStock!.Value - item.Quantity);

                // --- LÍNEA RESTAURADA ---
                // Ahora que GenericRepository.Update está corregido,
                // volvemos a llamar a Update explícitamente.
                _unitOfWork.ProductRepository.Update(product);

                var orderDetail = new OrderDetail
                {
                    // OrderID se asignará automáticamente por EF Core al guardar
                    ProductID = product.ProductID,
                    UnitPrice = product.UnitPrice ?? 0m, // Usar precio del producto
                    Quantity = item.Quantity,
                    Discount = 0 // Asumiendo sin descuento por ahora
                };
                newOrder.OrderDetails.Add(orderDetail); // Añadir detalle a la orden

                // Calcular subtotal de la línea (Precio * Cantidad * (1 - Descuento))
                decimal lineSubtotal = (orderDetail.UnitPrice * orderDetail.Quantity * (1 - (decimal)orderDetail.Discount));
                subtotalOrder += lineSubtotal; // Acumular subtotal de la orden
            }

            // --- 4. Calcular Totales Finales (Subtotal, IVA, Total) ---
            // Requisito 4.pdf"]
            newOrder.Subtotal = subtotalOrder;
            newOrder.VatAmount = Math.Round(subtotalOrder * VatRate, 2); // Calcula el monto del IVA y redondea a 2 decimales
            newOrder.TotalAmount = newOrder.Subtotal + newOrder.VatAmount + (newOrder.Freight ?? 0m); // Suma todo

            // --- 5. Guardar la Orden (Transaccional) ---
            await _unitOfWork.OrderRepository.AddAsync(newOrder); // Añadir la nueva orden al contexto

            // Requisito 17: "Utilizar transacciones".pdf"]
            // SaveChangesAsync guardará la Order (Added), los OrderDetails (Added)
            // y el Product (Modified) en una sola transacción.
            int changes = await _unitOfWork.SaveChangesAsync();

            if (changes > 0)
            {
                // Loguear éxito con detalles
                _logger.LogInfo($"Nueva orden creada con ID: {newOrder.OrderID}. Subtotal: {newOrder.Subtotal:N2}, IVA: {newOrder.VatAmount:N2}, Total: {newOrder.TotalAmount:N2}");
                return newOrder.OrderID; // Devuelve el ID de la nueva orden
            }
            else
            {
                // Loguear si SaveChangesAsync no reportó cambios (inesperado si AddAsync funcionó)
                _logger.LogError($"No se pudo guardar la orden para el cliente {request.CustomerID}. SaveChangesAsync devolvió 0.", null);
                throw new Exception("No se pudo guardar la orden. No se realizaron cambios en la base de datos.");
            }
        }
    }
}