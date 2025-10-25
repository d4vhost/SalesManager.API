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
            if (order == null || order.OrderDetails == null || !order.OrderDetails.Any())
            {
                throw new InvalidOperationException("No se puede generar PDF para una orden sin detalles.");
            }

            try
            {
                var document = new InvoiceDocument(order);
                byte[] pdfBytes = document.GeneratePdf();
                _logger.LogInfo($"PDF generado para la orden {order.OrderID}. Tamaño: {pdfBytes.Length} bytes.");
                return Task.FromResult(pdfBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al generar PDF para la orden {order.OrderID}.", ex);
                throw;
            }
        }
    }

    public class InvoiceDocument : IDocument
    {
        public Order Model { get; }

        public InvoiceDocument(Order model)
        {
            Model = model;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Margin(50);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily(Fonts.Arial));
                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
        }

        void ComposeHeader(IContainer container)
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
                row.ConstantItem(100).Height(50).Placeholder(); // Placeholder para logo
            });

            container.PaddingTop(20).Column(column => {
                column.Item().Text("Cliente:").SemiBold();
                column.Item().Text(Model.Customer?.CompanyName ?? "N/A");
                column.Item().Text(Model.Customer?.ContactName ?? "");
                column.Item().Text($"ID: {Model.CustomerID}");
                column.Item().Text($"Dirección Envío: {Model.ShipAddress ?? ""}, {Model.ShipCity ?? ""}, {Model.ShipCountry ?? ""}");
            });

            container.PaddingTop(10);
        }

        void ComposeContent(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25); // #
                    columns.RelativeColumn(3);  // Producto
                    columns.RelativeColumn();   // Precio Unit.
                    columns.RelativeColumn();   // Cantidad
                    columns.RelativeColumn();   // Descuento
                    columns.RelativeColumn();   // Subtotal
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("#");
                    header.Cell().Element(CellStyle).Text("Producto");
                    header.Cell().Element(CellStyle).AlignRight().Element(CellStyle).Text("P. Unit.");
                    header.Cell().Element(CellStyle).AlignRight().Element(CellStyle).Text("Cant.");
                    header.Cell().Element(CellStyle).AlignRight().Element(CellStyle).Text("Desc.");
                    header.Cell().Element(CellStyle).AlignRight().Element(CellStyle).Text("Subtotal");

                    static IContainer CellStyle(IContainer cellContainer) =>
                        cellContainer.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                });

                int index = 1;
                foreach (var item in Model.OrderDetails)
                {
                    var subtotal = (item.UnitPrice * item.Quantity * (1 - (decimal)item.Discount));
                    table.Cell().Element(CellStyle).Text((index++).ToString()); // Convertir a string
                    table.Cell().Element(CellStyle).Text(item.Product?.ProductName ?? "N/A");
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.UnitPrice:N2}");
                    table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString()); // Convertir a string
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.Discount:P0}");
                    table.Cell().Element(CellStyle).AlignRight().Text($"{subtotal:N2}");

                    static IContainer CellStyle(IContainer cellContainer) => cellContainer.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }

                var total = Model.OrderDetails.Sum(item => (item.UnitPrice * item.Quantity * (1 - (decimal)item.Discount)));
                table.Cell().ColumnSpan(5).AlignRight().Text("Total: ").SemiBold();
                table.Cell().AlignRight().Text($"{total:N2}").SemiBold();
            });
        }
    }
}