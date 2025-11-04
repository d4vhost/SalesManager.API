using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManager.BusinessObjects.Interfaces; // Necesario para IUnitOfWork y ILoggerService
using SalesManager.UseCases.DTOs.Common;
using SalesManager.UseCases.DTOs.Orders; // Necesario para DTOs de Órdenes
using SalesManager.UseCases.Features; // Necesario para CreateOrderInteractor
using SalesManager.UseCases.Interfaces; // Necesario para ILoggerService, IPdfGeneratorService
using System.Security.Claims;

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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // --- INICIO DE CAMBIOS ---
                // 1. Obtener el claim "employeeId" del token del usuario logueado
                var employeeIdClaim = User.FindFirstValue("employeeId");

                // 2. Convertirlo a un entero nullable
                int? employeeId = int.TryParse(employeeIdClaim, out int id) ? id : (int?)null;

                // 3. Pasar el ID al interactor
                int newOrderId = await _createOrderInteractor.HandleAsync(createOrderRequest, employeeId);
                // --- FIN DE CAMBIOS ---

                _logger.LogInfo($"Orden {newOrderId} creada exitosamente.");
                return Ok(new { orderId = newOrderId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado al crear orden.", ex);
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
                CustomerName = order.Customer?.ContactName, 
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

        // GET: api/Orders?customerId=ANATR&pageNumber=1&pageSize=10
        /// <summary>
        /// Busca y pagina las órdenes filtrando por cliente o empleado.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<OrderSummaryDto>>> GetOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? customerId = null,
            [FromQuery] int? employeeId = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var (orders, totalCount) = await _unitOfWork.OrderRepository.FindOrdersAsync(
                pageNumber,
                pageSize,
                customerId,
                employeeId);

            // Mapear a un DTO más simple para la lista
            var orderDtos = orders.Select(o => new OrderSummaryDto
            {
                OrderID = o.OrderID,
                OrderDate = o.OrderDate,
                CustomerID = o.CustomerID,
                CustomerName = o.Customer?.CompanyName ?? "N/A",
                TotalAmount = o.TotalAmount
            }).ToList();

            var pagedResult = new PagedResultDto<OrderSummaryDto>(orderDtos, pageNumber, pageSize, totalCount);
            return Ok(pagedResult);
        }

        // GET: api/Orders/{id}/pdf
        /// <summary>
        /// Genera y devuelve la factura de una orden en formato PDF.
        /// </summary>
        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> GetOrderPdf(int id)
        {
            try
            {
                _logger.LogInfo($"Intentando generar PDF para orden {id}");

                // Obtener la orden con todos los detalles necesarios
                var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(id);

                if (order == null)
                {
                    _logger.LogWarn($"Orden con ID {id} no encontrada.", null);
                    return NotFound(new { message = $"Orden con ID {id} no encontrada." });
                }

                // Validar que la orden tenga detalles
                if (order.OrderDetails == null || !order.OrderDetails.Any())
                {
                    _logger.LogWarn($"Orden {id} no tiene detalles para generar PDF.", null);
                    return BadRequest(new { message = "La orden no tiene productos para generar la factura." });
                }

                _logger.LogInfo($"Orden {id} encontrada. Detalles: {order.OrderDetails.Count}, Customer: {order.Customer?.CompanyName ?? "NULL"}");

                // Generar el PDF
                byte[] pdfBytes = await _pdfGeneratorService.GenerateInvoicePdfAsync(order);

                _logger.LogInfo($"PDF generado exitosamente para orden {id}. Tamaño: {pdfBytes.Length} bytes");

                // Devolver el archivo PDF
                return File(pdfBytes, "application/pdf", $"Factura-{id}.pdf");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"Error de operación al generar PDF para orden {id}: {ex.Message}", ex);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error inesperado al generar PDF para orden {id}.", ex);
                return StatusCode(500, new
                {
                    message = "Error interno al generar el PDF de la factura.",
                    details = ex.Message,
                    stackTrace = ex.StackTrace
                });
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

    /// <summary>
    /// DTO simplificado para listas de órdenes.
    /// </summary>
    public class OrderSummaryDto
    {
        public int OrderID { get; set; }
        public DateTime? OrderDate { get; set; }
        public string? CustomerID { get; set; }
        public string? CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
    }
}