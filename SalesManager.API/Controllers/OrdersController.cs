using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManager.BusinessObjects.Interfaces; // Necesario para IUnitOfWork y ILoggerService
using SalesManager.UseCases.DTOs.Orders; // Necesario para DTOs de Órdenes
using SalesManager.UseCases.Features; // Necesario para CreateOrderInteractor
using SalesManager.UseCases.Interfaces;
using System; // Necesario para Exception
using System.Collections.Generic; // Necesario para List<>
using System.Linq; // Necesario para .Select() y .Sum()
using System.Threading.Tasks; // Necesario para Task<>

namespace SalesManager.WebAPI.Controllers // Asegúrate que el namespace sea correcto
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Todas las acciones requieren login
    public class OrdersController : ControllerBase
    {
        private readonly CreateOrderInteractor _createOrderInteractor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _logger; // Para loguear errores

        public OrdersController(CreateOrderInteractor createOrderInteractor, IUnitOfWork unitOfWork, ILoggerService logger)
        {
            _createOrderInteractor = createOrderInteractor;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // POST: api/Orders
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto createOrderRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Delega toda la lógica de creación al Interactor
                int newOrderId = await _createOrderInteractor.HandleAsync(createOrderRequest);

                // Devuelve 200 OK con el ID de la nueva orden
                return Ok(new { orderId = newOrderId });
            }
            catch (InvalidOperationException ex) // Errores de negocio esperados (stock, duplicados, etc.).pdf"]
            {
                _logger.LogWarn($"Error de negocio al crear orden: {ex.Message}");
                // Devuelve 400 Bad Request con el mensaje de error específico
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) // Errores inesperados (problemas de DB, etc.)
            {
                _logger.LogError("Error inesperado al crear orden.", ex);
                // Devuelve 500 Internal Server Error con un mensaje genérico
                return StatusCode(500, new { message = "Ocurrió un error inesperado al procesar la orden." });
            }
        }

        // GET: api/Orders/{id} (ej: /api/Orders/10248)
        // Requisito 12: Mostrar encabezado y detalle de la orden.pdf"]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            // Usa el método específico del repositorio para traer la orden con sus detalles
            var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(id);

            if (order == null)
            {
                return NotFound($"Orden con ID {id} no encontrada.");
            }

            // --- Mapeo Manual a un DTO de respuesta detallado ---
            // (Necesitas crear las clases OrderDetailsResponseDto y OrderItemDetailsDto)
            var orderDetailsDto = new OrderDetailsResponseDto
            {
                OrderID = order.OrderID,
                OrderDate = order.OrderDate,
                CustomerID = order.CustomerID,
                CustomerName = order.Customer?.CompanyName, // Accede a la propiedad de navegación
                ShipAddress = order.ShipAddress,
                ShipCity = order.ShipCity,
                ShipCountry = order.ShipCountry,
                // Requisito 4: Calcular totales.pdf"] (Ejemplo simple, podrías tener IVA, etc.)
                TotalAmount = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity * (1 - (decimal)od.Discount)),
                Items = order.OrderDetails.Select(od => new OrderItemDetailsDto
                {
                    ProductID = od.ProductID,
                    ProductName = od.Product?.ProductName, // Accede a la propiedad de navegación
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    Discount = (decimal)od.Discount,
                    Subtotal = (od.UnitPrice * od.Quantity * (1 - (decimal)od.Discount))
                }).ToList()
            };

            return Ok(orderDetailsDto);
        }

        // (Opcional - Requisito 12: Generar PDF).pdf"]
        // GET: api/Orders/{id}/pdf
        // [HttpGet("{id}/pdf")]
        // public async Task<IActionResult> GetOrderPdf(int id)
        // {
        //     var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(id);
        //     if (order == null) return NotFound();
        //
        //     // --- Lógica para generar PDF ---
        //     // byte[] pdfBytes = await _pdfGeneratorService.GenerateInvoicePdfAsync(order); // Necesitarías un servicio para esto
        //
        //     // return File(pdfBytes, "application/pdf", $"Factura-{id}.pdf");
        //     return Ok("Funcionalidad PDF pendiente."); // Placeholder
        // }
    }

    // --- DTOs Auxiliares para GetOrder (Crea estos en SalesManager.UseCases/DTOs/Orders) ---
    public class OrderDetailsResponseDto // Cambié el nombre para evitar conflicto
    {
        public int OrderID { get; set; }
        public DateTime? OrderDate { get; set; }
        public string? CustomerID { get; set; }
        public string? CustomerName { get; set; }
        public string? ShipAddress { get; set; }
        public string? ShipCity { get; set; }
        public string? ShipCountry { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItemDetailsDto> Items { get; set; } = new List<OrderItemDetailsDto>();
    }

    public class OrderItemDetailsDto
    {
        public int ProductID { get; set; }
        public string? ProductName { get; set; }
        public short Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Subtotal { get; set; }
    }
}