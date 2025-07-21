using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;

namespace Application.Interfaces.User
{
    public interface IUserDetailsService
    {
        ResponseVM SaveUserDetails(int userId, UserDetailsVM userDetails);
        ResponseVM GetUserDetails(int userId);
    }
}
