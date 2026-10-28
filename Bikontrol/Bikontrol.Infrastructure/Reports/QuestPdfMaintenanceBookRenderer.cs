using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Bikontrol.Infrastructure.Reports
{
    /// <summary>QuestPDF implementation of <see cref="IMaintenanceBookPdfRenderer"/>.</summary>
    public class QuestPdfMaintenanceBookRenderer : IMaintenanceBookPdfRenderer
    {
        public byte[] Render(MaintenanceBookDTO book)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(t => t.FontSize(10).FontFamily(Fonts.Calibri));

                    page.Header().Column(header =>
                    {
                        header.Item().Text("Libro de mantenimiento").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                        header.Item().Text($"{book.Brand} {book.Name} ({book.Year})").FontSize(12).SemiBold();
                        var info = $"Apodo: {book.Nickname}  ·  Cilindrada: {book.Displacement} cc  ·  Patente: {book.Plate}";
                        var km = $"Odómetro actual: {book.CurrentKm} km" +
                                 (book.RidingSince is not null ? $"  ·  En uso desde: {book.RidingSince:yyyy-MM-dd}" : string.Empty);
                        header.Item().Text(info).FontSize(10).FontColor(Colors.Grey.Darken2);
                        header.Item().Text(km).FontSize(10).FontColor(Colors.Grey.Darken2);
                        header.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(75);   // fecha
                            columns.ConstantColumn(60);   // km
                            columns.RelativeColumn(3);    // mantenimiento
                            columns.RelativeColumn(2);    // intervalo
                        });

                        table.Header(head =>
                        {
                            head.Cell().Element(HeaderCell).Text("Fecha");
                            head.Cell().Element(HeaderCell).Text("Odómetro");
                            head.Cell().Element(HeaderCell).Text("Mantenimiento");
                            head.Cell().Element(HeaderCell).Text("Intervalo");
                        });

                        if (book.Entries.Count == 0)
                        {
                            table.Cell().ColumnSpan(4).Padding(8).Text("Todavía no hay mantenimientos registrados.")
                                .FontColor(Colors.Grey.Darken1).Italic();
                        }

                        foreach (var entry in book.Entries)
                        {
                            table.Cell().Element(BodyCell).Text(entry.PerformedAt.ToString("yyyy-MM-dd"));
                            table.Cell().Element(BodyCell).Text(entry.PerformedKm.HasValue ? $"{entry.PerformedKm} km" : "—");
                            table.Cell().Element(BodyCell).Text(entry.Name);
                            table.Cell().Element(BodyCell).Text(FormatInterval(entry));
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generado con Bikontrol — ");
                        text.Span($"{DateTime.UtcNow:yyyy-MM-dd}").SemiBold();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static string FormatInterval(MaintenanceBookEntryDTO entry)
        {
            if (entry.TrackingType == "Time" && entry.TimeIntervalWeeks is > 0)
                return $"cada {entry.TimeIntervalWeeks} sem";
            if (entry.KmInterval is > 0)
                return $"cada {entry.KmInterval} km";
            return "—";
        }

        private static IContainer HeaderCell(IContainer container) =>
            container.Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).DefaultTextStyle(t => t.SemiBold());

        private static IContainer BodyCell(IContainer container) =>
            container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(3);
    }
}
