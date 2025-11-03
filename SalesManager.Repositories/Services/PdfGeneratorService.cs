using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SalesManager.BusinessObjects.Entities;
using SalesManager.UseCases.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.Repositories.Services
{
    public class PdfGeneratorService : IPdfGeneratorService
    {
        private readonly ILoggerService _logger;

        public PdfGeneratorService(ILoggerService logger)
        {
            _logger = logger;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public Task<byte[]> GenerateInvoicePdfAsync(Order order)
        {
            try
            {
                _logger.LogInfo($"=== INICIO GenerateInvoicePdfAsync para orden {order?.OrderID} ===");

                if (order == null)
                {
                    _logger.LogError("ERROR: order es NULL", null);
                    throw new InvalidOperationException("Order es null");
                }

                _logger.LogInfo($"Order ID: {order.OrderID}");
                _logger.LogInfo($"Customer: {order.Customer?.CompanyName ?? "NULL"}");
                _logger.LogInfo($"OrderDetails count: {order.OrderDetails?.Count ?? 0}");

                if (order.OrderDetails == null)
                {
                    _logger.LogError($"ERROR: OrderDetails es NULL para orden {order.OrderID}", null);
                    throw new InvalidOperationException("OrderDetails es null");
                }

                if (!order.OrderDetails.Any())
                {
                    _logger.LogError($"ERROR: OrderDetails está VACÍO para orden {order.OrderID}", null);
                    throw new InvalidOperationException("No se puede generar PDF para una orden sin detalles.");
                }

                _logger.LogInfo("Creando InvoiceDocument...");
                var document = new InvoiceDocument(order, _logger);

                _logger.LogInfo("Generando PDF...");
                byte[] pdfBytes = document.GeneratePdf();

                _logger.LogInfo($"PDF generado exitosamente. Tamaño: {pdfBytes.Length} bytes");
                return Task.FromResult(pdfBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"EXCEPCIÓN en GenerateInvoicePdfAsync: {ex.GetType().Name} - {ex.Message}", ex);
                throw;
            }
        }
    }

    public class InvoiceDocument : IDocument
    {
        public Order Model { get; }
        private readonly ILoggerService _logger;

        public InvoiceDocument(Order model, ILoggerService logger)
        {
            Model = model;
            _logger = logger;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            try
            {
                _logger.LogInfo("=== INICIO Compose ===");

                container.Page(page =>
                {
                    _logger.LogInfo("Configurando página...");
                    page.Margin(50);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily(Fonts.Arial));

                    _logger.LogInfo("Componiendo header...");
                    page.Header().Element(ComposeHeader);

                    _logger.LogInfo("Componiendo content...");
                    page.Content().Element(ComposeContent);

                    _logger.LogInfo("Componiendo footer...");
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });

                    _logger.LogInfo("Página configurada exitosamente");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR en Compose: {ex.Message}", ex);
                throw;
            }
        }

        void ComposeHeader(IContainer container)
        {
            try
            {
                var titleStyle = TextStyle.Default.FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

                container.Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text($"Factura #{Model.OrderID}").Style(titleStyle);
                        column.Item().Text(text =>
                        {
                            text.Span("Fecha Orden: ").SemiBold();
                            text.Span($"{Model.OrderDate:yyyy-MM-dd}");
                        });
                    });
                    row.ConstantItem(100).Height(50).Placeholder();
                });

                container.PaddingTop(20).Column(column => {
                    column.Item().Text("Cliente:").SemiBold();
                    column.Item().Text(Model.Customer?.CompanyName ?? "N/A");
                    column.Item().Text(Model.Customer?.ContactName ?? "");
                    column.Item().Text($"ID: {Model.CustomerID}");
                    column.Item().Text($"Dirección: {Model.ShipAddress ?? ""}, {Model.ShipCity ?? ""}, {Model.ShipCountry ?? ""}");
                });

                container.PaddingTop(10);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR en ComposeHeader: {ex.Message}", ex);
                throw;
            }
        }

        void ComposeContent(IContainer container)
        {
            try
            {
                _logger.LogInfo($"ComposeContent: Procesando {Model.OrderDetails.Count} items");

                container.Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCellStyle).Text("#");
                        header.Cell().Element(HeaderCellStyle).Text("Producto");
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("P. Unit.");
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("Cant.");
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("Desc.");
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("Subtotal");
                    });

                    int index = 1;
                    foreach (var item in Model.OrderDetails)
                    {
                        try
                        {
                            _logger.LogInfo($"Procesando item {index}: ProductID={item.ProductID}, ProductName={item.Product?.ProductName ?? "NULL"}");

                            var unitPrice = item.UnitPrice;
                            var quantity = item.Quantity;
                            var discount = (decimal)item.Discount;
                            var subtotal = unitPrice * quantity * (1 - discount);

                            table.Cell().Element(BodyCellStyle).Text(index.ToString());
                            table.Cell().Element(BodyCellStyle).Text(item.Product?.ProductName ?? "Producto no encontrado");
                            table.Cell().Element(BodyCellStyle).AlignRight().Text($"{unitPrice:N2}");
                            table.Cell().Element(BodyCellStyle).AlignRight().Text(quantity.ToString());
                            table.Cell().Element(BodyCellStyle).AlignRight().Text($"{discount:P0}");
                            table.Cell().Element(BodyCellStyle).AlignRight().Text($"{subtotal:N2}");

                            index++;
                        }
                        catch (Exception itemEx)
                        {
                            _logger.LogError($"ERROR procesando item {index}: {itemEx.Message}", itemEx);
                            throw;
                        }
                    }

                    // Totales
                    table.Cell().ColumnSpan(5).AlignRight().PaddingRight(10).Text("Subtotal: ").SemiBold();
                    table.Cell().AlignRight().Element(BodyCellStyle).Text($"{Model.Subtotal:N2}").SemiBold();

                    table.Cell().ColumnSpan(5).AlignRight().PaddingRight(10).Text("IVA (12%): ").SemiBold();
                    table.Cell().AlignRight().Element(BodyCellStyle).Text($"{Model.VatAmount:N2}").SemiBold();

                    table.Cell().ColumnSpan(5).AlignRight().PaddingRight(10).Text("Flete: ").SemiBold();
                    table.Cell().AlignRight().Element(BodyCellStyle).Text($"{Model.Freight ?? 0:N2}").SemiBold();

                    table.Cell().ColumnSpan(5).AlignRight().PaddingRight(10).Text("Total: ").Bold();
                    table.Cell().AlignRight().Element(TotalCellStyle).Text($"{Model.TotalAmount:N2}").Bold();
                });

                _logger.LogInfo("ComposeContent completado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR en ComposeContent: {ex.Message}", ex);
                throw;
            }
        }

        static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .DefaultTextStyle(x => x.SemiBold())
                .PaddingVertical(5)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2);
        }

        static IContainer BodyCellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(5);
        }

        static IContainer TotalCellStyle(IContainer container)
        {
            return container
                .BorderBottom(2)
                .BorderTop(2)
                .BorderColor(Colors.Blue.Medium)
                .PaddingVertical(8)
                .Background(Colors.Blue.Lighten5);
        }
    }
}