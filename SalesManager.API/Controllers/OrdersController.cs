using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManager.BusinessObjects.Interfaces; // Necesario para IUnitOfWork y ILoggerService
using SalesManager.UseCases.DTOs.Orders; // Necesario para DTOs de Órdenes
using SalesManager.UseCases.Features; // Necesario para CreateOrderInteractor
using SalesManager.UseCases.Interfaces; // Necesario para ILoggerService, IPdfGeneratorService
using System; // Necesario para Exception, DateTime
using System.Collections.Generic; // Necesario para List<>
using System.Linq; // Necesario para .Select() y .Sum()
using System.Threading.Tasks; // Necesario para Task<>

namespace SalesManager.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Todas las acciones requieren login por defecto
    public class OrdersController : ControllerBase
    {
        private readonly CreateOrderInteractor _createOrderInteractor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILoggerService _logger;
        private readonly IPdfGeneratorService _pdfGeneratorService;

        public OrdersController(
            CreateOrderInteractor createOrderInteractor,
            IUnitOfWork unitOfWork,
            ILoggerService logger,
            IPdfGeneratorService pdfGeneratorService)
        {
            _createOrderInteractor = createOrderInteractor;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _pdfGeneratorService = pdfGeneratorService;
        }

        // POST: api/Orders
        /// <summary>
        /// Crea una nueva orden de venta.
        /// </summary>
        /// <param name="createOrderRequest">Datos de la orden a crear.</param>
        /// <returns>El ID de la orden creada o un error.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto createOrderRequest)
        {
            // Valida el modelo de entrada usando FluentValidation y DataAnnotations
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Delega la lógica principal al Interactor
                int newOrderId = await _createOrderInteractor.HandleAsync(createOrderRequest);

                // Devuelve 200 OK con el ID de la nueva orden
                _logger.LogInfo($"Orden {newOrderId} creada exitosamente.");
                return Ok(new { orderId = newOrderId });
            }
            catch (InvalidOperationException ex) // Errores de negocio esperados (stock, duplicados, etc.)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) // Errores inesperados
            {
                _logger.LogError("Error inesperado al crear orden.", ex);
                // Devuelve 500 Internal Server Error
                return StatusCode(500, new { message = "Ocurrió un error inesperado al procesar la orden." });
            }
        }

        // GET: api/Orders/{id}
        /// <summary>
        /// Obtiene los detalles completos de una orden específica.
        /// </summary>
        /// <param name="id">El ID de la orden.</param>
        /// <returns>Los detalles de la orden o un error 404.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            // Usa el método específico del repositorio que incluye los detalles
            var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(id);

            if (order == null)
            {
                return NotFound($"Orden con ID {id} no encontrada.");
            }

            // --- Mapeo a DTO de Respuesta Actualizado ---
            var orderDetailsDto = new OrderDetailsResponseDto
            {
                OrderID = order.OrderID,
                OrderDate = order.OrderDate,
                CustomerID = order.CustomerID,
                CustomerName = order.Customer?.CompanyName, // Asume que Customer fue incluido
                ShipAddress = order.ShipAddress,
                ShipCity = order.ShipCity,
                ShipCountry = order.ShipCountry,
                // Nuevos campos para mostrar los totales calculados
                Subtotal = order.Subtotal,
                VatAmount = order.VatAmount,
                Freight = order.Freight ?? 0m, // Usa el Freight guardado
                TotalAmount = order.TotalAmount, // Usa el Total guardado
                Items = order.OrderDetails.Select(od => new OrderItemDetailsDto
                {
                    ProductID = od.ProductID,
                    ProductName = od.Product?.ProductName, // Asume que Product fue incluido
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                    Discount = (decimal)od.Discount,
                    Subtotal = (od.UnitPrice * od.Quantity * (1 - (decimal)od.Discount)) // Subtotal por línea
                }).ToList()
            };

            return Ok(orderDetailsDto);
        }

        // GET: api/Orders/{id}/pdf
        /// <summary>
        /// Genera y devuelve la factura de una orden en formato PDF.
        /// </summary>
        /// <param name="id">El ID de la orden.</param>
        /// <returns>Un archivo PDF o un error.</returns>
        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> GetOrderPdf(int id)
        {
            // Obtener la orden con detalles necesarios para el PDF
            var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(id);
            if (order == null)
            {
                return NotFound($"Orden con ID {id} no encontrada.");
            }

            try
            {
                // Generar el PDF usando el servicio inyectado
                byte[] pdfBytes = await _pdfGeneratorService.GenerateInvoicePdfAsync(order);

                // Devolver el archivo PDF con el nombre adecuado
                return File(pdfBytes, "application/pdf", $"Factura-{id}.pdf");
            }
            catch (InvalidOperationException ex) // Ej: Orden sin detalles
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex) // Otros errores durante la generación
            {
                _logger.LogError($"Error inesperado al generar PDF para orden {id}.", ex);
                return StatusCode(500, new { message = "Error al generar el PDF de la factura." });
            }
        }
    }

    // --- DTOs Auxiliares para GetOrder ---
    // NOTA: Idealmente, estas clases deberían moverse a SalesManager.UseCases/DTOs/Orders/

    public class OrderDetailsResponseDto
    {
        public int OrderID { get; set; }
        public DateTime? OrderDate { get; set; }
        public string? CustomerID { get; set; }
        public string? CustomerName { get; set; }
        public string? ShipAddress { get; set; }
        public string? ShipCity { get; set; }
        public string? ShipCountry { get; set; }
        // --- Campos de Totales ---
        public decimal Subtotal { get; set; }
        public decimal VatAmount { get; set; }
        public decimal Freight { get; set; }
        public decimal TotalAmount { get; set; } // Total general
        public List<OrderItemDetailsDto> Items { get; set; } = new List<OrderItemDetailsDto>();
    }

    public class OrderItemDetailsDto
    {
        public int ProductID { get; set; }
        public string? ProductName { get; set; }
        public short Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Subtotal { get; set; } // Subtotal por línea
    }
}