using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;

namespace Application.Interfaces.User
{
    public interface IUserDetailsService
    {
        ResponseVM SaveUserDetails(UserDetailsVM userDetails);
    }
}
