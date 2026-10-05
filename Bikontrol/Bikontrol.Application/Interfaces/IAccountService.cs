using Bikontrol.Application.DTOs.Users;

namespace Bikontrol.Application.Interfaces
{
    public interface IAccountService
    {
        /// <summary>
        /// Returns everything the current user owns as a single document, for the
        /// "download my data" flow.
        /// </summary>
        Task<UserDataExportDTO> ExportMyDataAsync();
    }
}
