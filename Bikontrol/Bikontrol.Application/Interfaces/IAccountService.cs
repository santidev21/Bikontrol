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

        /// <summary>
        /// Irreversibly deletes the current user's account: purges owned data and
        /// anonymizes the account row. Requires the confirmation word and, for
        /// password accounts, the current password.
        /// </summary>
        Task DeleteMyAccountAsync(DeleteAccountRequest request);
    }
}
