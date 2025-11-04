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
                if (order.OrderDetails == null || !order.OrderDetails.Any())
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

    // --- PLANTILLA PROFESIONAL CON DISEÑO MEJORADO ---
    public class InvoiceDocument : IDocument
    {
        public Order Model { get; }
        private readonly ILoggerService _logger;

        // Colores profesionales
        const string PrimaryColor = "#1E8449";      // Verde corporativo
        const string SecondaryColor = "#27AE60";    // Verde más claro
        const string LightGrey = "#F8F9FA";         // Gris muy claro para fondos
        const string MediumGrey = "#E8E8E8";        // Gris medio para bordes
        const string DarkGrey = "#2C3E50";          // Gris oscuro para texto
        const string BorderColor = "#CCCCCC";       // Color de bordes de tabla

        // Icono SVG de Factura Profesional (Documento con líneas)
        static readonly string InvoiceIcon = @"<svg viewBox=""0 0 24 24"" fill=""currentColor"">
            <path d=""M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20M10,19L12,15H9V10H13V15L11,19H10Z""/>
        </svg>";

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
                container.Page(page =>
                {
                    page.Margin(40f);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10f).FontFamily(Fonts.Arial).FontColor(DarkGrey));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
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
            var titleStyle = TextStyle.Default.FontSize(28f).ExtraBold().FontColor(PrimaryColor);

            container.Column(column =>
            {
                // Barra superior decorativa
                column.Item().Height(5f).Background(PrimaryColor);

                column.Item().PaddingTop(15f).Row(row =>
                {
                    // Columna 1: Icono y Título
                    row.RelativeItem().Row(iconRow =>
                    {
                        iconRow.ConstantItem(45f).Height(45f).Svg(InvoiceIcon);
                        iconRow.ConstantItem(10f);
                        iconRow.RelativeItem().Column(col =>
                        {
                            col.Item().Text("FACTURA").Style(titleStyle);
                            col.Item().PaddingTop(2f).Text($"Orden #{Model.OrderID}")
                                .FontSize(11f).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    // Columna 2: Datos de la Empresa
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text("SalesManager").Bold().FontSize(16f).FontColor(DarkGrey);
                        col.Item().PaddingTop(4f).Text("Av. Amazonas y Colón").FontSize(9f);
                        col.Item().Text("Ed. SalesManager").FontSize(9f);
                        col.Item().Text("Quito, Ecuador").FontSize(9f);
                        col.Item().PaddingTop(3f).Text("facturacion@salesmanager.com")
                            .FontSize(9f).FontColor(PrimaryColor).Italic();
                    });
                });

                // Línea separadora
                column.Item().PaddingTop(15f).LineHorizontal(2f).LineColor(SecondaryColor);
            });
        }

        void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                // --- SECCIÓN 1: DATOS DEL CLIENTE Y FECHA ---
                column.Item().PaddingTop(20f).Row(row =>
                {
                    // Izquierda: Cliente
                    row.RelativeItem().Background(LightGrey).Border(1f).BorderColor(MediumGrey)
                        .Padding(10f).Column(col =>
                        {
                            col.Item().Text("FACTURAR A:").SemiBold().FontSize(9f).FontColor(PrimaryColor);
                            col.Item().PaddingTop(4f).Text(Model.Customer?.CompanyName ?? "N/A").Bold().FontSize(10f);
                            col.Item().Text(Model.Customer?.ContactName ?? "").FontSize(8f);
                            col.Item().Text(Model.Customer?.Address ?? "").FontSize(8f);
                            col.Item().Text($"{Model.Customer?.City ?? ""}, {Model.Customer?.Country ?? ""}").FontSize(8f);
                        });

                    row.ConstantItem(15f);

                    // Derecha: Envío y Fecha
                    row.RelativeItem().Column(col =>
                    {
                        // Envío
                        col.Item().Background(LightGrey).Border(1f).BorderColor(MediumGrey)
                            .Padding(10f).Column(shipCol =>
                            {
                                shipCol.Item().Text("ENVIAR A:").SemiBold().FontSize(9f).FontColor(PrimaryColor);
                                shipCol.Item().PaddingTop(4f).Text(Model.ShipName ?? Model.Customer?.CompanyName ?? "N/A").Bold().FontSize(10f);
                                shipCol.Item().Text(Model.ShipAddress ?? Model.Customer?.Address ?? "").FontSize(8f);
                                shipCol.Item().Text($"{Model.ShipCity ?? Model.Customer?.City ?? ""}, {Model.ShipCountry ?? Model.Customer?.Country ?? ""}").FontSize(8f);
                            });

                        // Fecha
                        col.Item().PaddingTop(8f).Background(PrimaryColor).Padding(5f)
                            .Text($"Fecha de Emisión: {Model.OrderDate:dd/MM/yyyy}")
                            .FontSize(8f).SemiBold().FontColor(Colors.White);
                    });
                });

                // --- SECCIÓN 2: TABLA DE PRODUCTOS CON BORDES PROFESIONALES ---
                column.Item().PaddingTop(20f).Element(ComposeTable);

                // --- SECCIÓN 3: TOTALES (Justo después de la tabla) ---
                column.Item().PaddingTop(15f).AlignRight().Element(ComposeTotals);
            });
        }

        void ComposeTable(IContainer container)
        {
            container.Border(2f).BorderColor(BorderColor).Table(table =>
            {
                // Definición de columnas
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25f);  // #
                    columns.RelativeColumn(5f);   // Producto
                    columns.RelativeColumn(1.5f); // P. Unit.
                    columns.RelativeColumn(1f);   // Cant.
                    columns.RelativeColumn(1f);   // Desc.
                    columns.RelativeColumn(1.5f); // Subtotal
                });

                // ENCABEZADO DE LA TABLA con fondo
                table.Header(header =>
                {
                    IContainer HeaderCellStyle(IContainer c) => c
                        .Background(PrimaryColor)
                        .Border(1f)
                        .BorderColor(BorderColor)
                        .Padding(4f)
                        .DefaultTextStyle(x => x.SemiBold().FontSize(8f).FontColor(Colors.White));

                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("#");
                    header.Cell().Element(HeaderCellStyle).Text("PRODUCTO");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("P. UNIT.");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("CANT.");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("DESC.");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("SUBTOTAL");
                });

                // FILAS DE PRODUCTOS con bordes visibles
                int index = 1;
                foreach (var item in Model.OrderDetails)
                {
                    var isEven = index % 2 == 0;
                    var bgColor = isEven ? LightGrey : "#FFFFFF";

                    IContainer BodyCellStyle(IContainer c) => c
                        .Background(bgColor)
                        .Border(1f)
                        .BorderColor(BorderColor)
                        .Padding(4f)
                        .DefaultTextStyle(x => x.FontSize(8f));

                    var lineSubtotal = item.UnitPrice * item.Quantity * (1 - (decimal)item.Discount);

                    table.Cell().Element(BodyCellStyle).AlignCenter().Text(index.ToString()).SemiBold();
                    table.Cell().Element(BodyCellStyle).Text(item.Product?.ProductName ?? "N/A");
                    table.Cell().Element(BodyCellStyle).AlignRight().Text($"${item.UnitPrice:N2}");
                    table.Cell().Element(BodyCellStyle).AlignCenter().Text(item.Quantity.ToString());
                    table.Cell().Element(BodyCellStyle).AlignCenter().Text($"{item.Discount:P0}");
                    table.Cell().Element(BodyCellStyle).AlignRight().Text($"${lineSubtotal:N2}").SemiBold();

                    index++;
                }

                // FILAS VACÍAS para llenar el espacio (mínimo 12 filas totales)
                int minRows = 12;
                int currentRows = Model.OrderDetails.Count();

                for (int i = currentRows; i < minRows; i++)
                {
                    var isEven = (i + 1) % 2 == 0;
                    var bgColor = isEven ? LightGrey : "#FFFFFF";

                    IContainer EmptyCellStyle(IContainer c) => c
                        .Background(bgColor)
                        .Border(1f)
                        .BorderColor(BorderColor)
                        .Padding(4f)
                        .Height(20f); // Altura fija para filas vacías

                    table.Cell().Element(EmptyCellStyle).Text("");
                    table.Cell().Element(EmptyCellStyle).Text("");
                    table.Cell().Element(EmptyCellStyle).Text("");
                    table.Cell().Element(EmptyCellStyle).Text("");
                    table.Cell().Element(EmptyCellStyle).Text("");
                    table.Cell().Element(EmptyCellStyle).Text("");
                }
            });
        }

        void ComposeTotals(IContainer container)
        {
            container.Width(190f).Border(1f).BorderColor(BorderColor).Column(column =>
            {
                // Estilo para filas de subtotal e IVA
                IContainer RowStyle(IContainer c) => c
                    .Border(1f)
                    .BorderColor(BorderColor)
                    .Padding(5f)
                    .Background(LightGrey);

                // Subtotal
                column.Item().Element(RowStyle).Row(row =>
                {
                    row.RelativeItem().Text("Subtotal:").FontSize(8f).FontColor(DarkGrey);
                    row.RelativeItem().AlignRight().Text($"${Model.Subtotal:N2}").FontSize(8f).SemiBold();
                });

                // IVA
                column.Item().Element(RowStyle).Row(row =>
                {
                    row.RelativeItem().Text("IVA (12%):").FontSize(8f).FontColor(DarkGrey);
                    row.RelativeItem().AlignRight().Text($"${Model.VatAmount:N2}").FontSize(8f).SemiBold();
                });

                // Flete
                column.Item().Element(RowStyle).Row(row =>
                {
                    row.RelativeItem().Text("Flete:").FontSize(8f).FontColor(DarkGrey);
                    row.RelativeItem().AlignRight().Text($"${Model.Freight ?? 0:N2}").FontSize(8f).SemiBold();
                });

                // TOTAL con destacado
                column.Item().Background(PrimaryColor).Padding(6f).Row(row =>
                {
                    row.RelativeItem().Text("TOTAL:").FontSize(10f).ExtraBold().FontColor(Colors.White);
                    row.RelativeItem().AlignRight().Text($"${Model.TotalAmount:N2}")
                        .FontSize(11f).ExtraBold().FontColor(Colors.White);
                });
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().LineHorizontal(1f).LineColor(MediumGrey);
                column.Item().PaddingTop(10f).Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(9f).FontColor(Colors.Grey.Darken1));
                    text.Span("¡Gracias por su compra! ");
                    text.Span("• ");
                    text.Span("SalesManager © 2025").SemiBold();
                });
                column.Item().PaddingTop(3f).Text("www.salesmanager.com")
                    .FontSize(8f).FontColor(PrimaryColor).Italic();
            });
        }
    }
}