using System.Globalization;
using System.Text;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Shared.Exceptions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Bikontrol.Infrastructure.Services
{
    public class MaintenanceBookService : IMaintenanceBookService
    {
        private readonly ICurrentUserService _current;
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly IUserMaintenanceRepository _userMaintenanceRepository;
        private readonly IMotorcycleMaintenanceRecordRepository _recordRepository;
        private readonly IKmHistoryService _kmHistoryService;

        public MaintenanceBookService(
            ICurrentUserService current,
            IMotorcycleRepository motorcycleRepository,
            IUserMaintenanceRepository userMaintenanceRepository,
            IMotorcycleMaintenanceRecordRepository recordRepository,
            IKmHistoryService kmHistoryService)
        {
            _current = current;
            _motorcycleRepository = motorcycleRepository;
            _userMaintenanceRepository = userMaintenanceRepository;
            _recordRepository = recordRepository;
            _kmHistoryService = kmHistoryService;
        }

        public async Task<MaintenanceBookDTO> GetBookAsync(Guid motorcycleId)
        {
            var motorcycle = await _motorcycleRepository.GetByIdAsync(motorcycleId);
            if (motorcycle is null)
                throw new NotFoundException("Motocicleta no encontrada.");
            if (motorcycle.UserId != _current.UserId)
                throw new ForbiddenAccessException("No tienes permisos para esta motocicleta.");

            // Names/intervals come from the (possibly deleted) user maintenance;
            // records already carry the name via the joined entity.
            var records = (await _recordRepository.GetByMotorcycleIdAsync(motorcycleId)).ToList();
            var currentKm = await _kmHistoryService.GetCurrentKmAsync(motorcycleId);
            var ridingSince = await _kmHistoryService.GetInitialRecordedAtAsync(motorcycleId);

            var book = new MaintenanceBookDTO
            {
                MotorcycleId = motorcycle.Id,
                Name = motorcycle.Name,
                Brand = motorcycle.Brand,
                Year = motorcycle.Year,
                Nickname = motorcycle.Nickname,
                Displacement = motorcycle.Displacement,
                Plate = motorcycle.Plate,
                CurrentKm = currentKm,
                RidingSince = ridingSince,
                Entries = records
                    .OrderByDescending(r => r.PerformedAt)
                    .ThenByDescending(r => r.CreatedAt)
                    .Select(r => new MaintenanceBookEntryDTO
                    {
                        PerformedAt = r.PerformedAt,
                        PerformedKm = r.PerformedKm,
                        Name = r.UserMaintenance?.Name ?? "Mantenimiento",
                        Description = r.UserMaintenance?.Description,
                        TrackingType = r.UserMaintenance?.TrackingType ?? "Km",
                        KmInterval = r.UserMaintenance?.KmInterval,
                        TimeIntervalWeeks = r.UserMaintenance?.TimeIntervalWeeks
                    })
                    .ToList()
            };

            return book;
        }

        public async Task<byte[]> GetCsvAsync(Guid motorcycleId)
        {
            var book = await GetBookAsync(motorcycleId);

            var builder = new StringBuilder();
            builder.AppendLine("Fecha,Odometro (km),Mantenimiento,Descripcion,Tipo,Intervalo km,Semanas");
            foreach (var entry in book.Entries)
            {
                builder.Append(entry.PerformedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                builder.Append(',');
                builder.Append(entry.PerformedKm?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
                builder.Append(',');
                builder.Append(Escape(entry.Name));
                builder.Append(',');
                builder.Append(Escape(entry.Description ?? string.Empty));
                builder.Append(',');
                builder.Append(Escape(entry.TrackingType));
                builder.Append(',');
                builder.Append(entry.KmInterval?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
                builder.Append(',');
                builder.Append(entry.TimeIntervalWeeks?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
                builder.AppendLine();
            }

            // UTF-8 BOM so Excel opens the accents correctly.
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        }

        public async Task<byte[]> GetPdfAsync(Guid motorcycleId)
        {
            var book = await GetBookAsync(motorcycleId);
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

        private static string Escape(string value) =>
            value.Contains(',') || value.Contains('"') || value.Contains('\n')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;

        private static IContainer HeaderCell(IContainer container) =>
            container.Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(3).DefaultTextStyle(t => t.SemiBold());

        private static IContainer BodyCell(IContainer container) =>
            container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(3);
    }
}
