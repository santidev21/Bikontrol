using Bikontrol.Application.DTOs.Motorcycle;

namespace Bikontrol.Application.Interfaces
{
    public interface IMaintenanceBookService
    {
        /// <summary>Builds the maintenance book for one of the user's motorcycles.</summary>
        Task<MaintenanceBookDTO> GetBookAsync(Guid motorcycleId);

        /// <summary>Renders the book as CSV (UTF-8, comma-separated).</summary>
        Task<byte[]> GetCsvAsync(Guid motorcycleId);

        /// <summary>Renders the book as a shareable PDF.</summary>
        Task<byte[]> GetPdfAsync(Guid motorcycleId);
    }
}
