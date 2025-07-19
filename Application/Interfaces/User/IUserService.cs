using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;

namespace Application.Interfaces.User
{
    public interface IUserService
    {
        ResponseVM SignUp(RegisterUserVM user);
        ResponseVM SignIn(LoginUserVM user);
        ResponseVM GetUserById(int userId);
        ResponseVM ChangePassword(ChangePasswordVM user);
        ResponseVM DeleteUser(int userId);
    }
}
