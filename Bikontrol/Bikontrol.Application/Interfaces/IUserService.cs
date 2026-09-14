using Bikontrol.Application.DTOs.Users;
using System.Threading.Tasks;

namespace Bikontrol.Application.Interfaces
{
    public interface IUserService
    {
        Task<ProfileDTO> GetMeAsync();
        Task<ProfileDTO> UpdateProfileAsync(UpdateProfileRequest request);
        Task ChangePasswordAsync(ChangePasswordRequest request);
    }
}
