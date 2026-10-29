using System.Globalization;
using System.Text;
using Bikontrol.Application.DTOs.Motorcycle;
using Bikontrol.Application.Interfaces;
using Bikontrol.Application.Interfaces.Repositories;
using Bikontrol.Shared.Exceptions;

namespace Bikontrol.Application.Services
{
    public class MaintenanceBookService : IMaintenanceBookService
    {
        private readonly ICurrentUserService _current;
        private readonly IMotorcycleRepository _motorcycleRepository;
        private readonly IUserMaintenanceRepository _userMaintenanceRepository;
        private readonly IMotorcycleMaintenanceRecordRepository _recordRepository;
        private readonly IKmHistoryService _kmHistoryService;
        private readonly IMaintenanceBookPdfRenderer _pdfRenderer;

        public MaintenanceBookService(
            ICurrentUserService current,
            IMotorcycleRepository motorcycleRepository,
            IUserMaintenanceRepository userMaintenanceRepository,
            IMotorcycleMaintenanceRecordRepository recordRepository,
            IKmHistoryService kmHistoryService,
            IMaintenanceBookPdfRenderer pdfRenderer)
        {
            _current = current;
            _motorcycleRepository = motorcycleRepository;
            _userMaintenanceRepository = userMaintenanceRepository;
            _recordRepository = recordRepository;
            _kmHistoryService = kmHistoryService;
            _pdfRenderer = pdfRenderer;
        }

        public async Task<MaintenanceBookDTO> GetBookAsync(Guid motorcycleId)
        {
            var motorcycle = await MotorcycleOwnership.EnsureOwnedAsync(_motorcycleRepository, _current.UserId, motorcycleId);

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
            return _pdfRenderer.Render(book);
        }

        private static string Escape(string value) =>
            value.Contains(',') || value.Contains('"') || value.Contains('\n')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
    }
}
