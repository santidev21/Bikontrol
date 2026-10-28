using Bikontrol.Application.DTOs.Motorcycle;

namespace Bikontrol.Application.Interfaces
{
    /// <summary>Renders a maintenance book as a PDF document.</summary>
    public interface IMaintenanceBookPdfRenderer
    {
        byte[] Render(MaintenanceBookDTO book);
    }
}
