using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;

namespace Application.Interfaces.User
{
    public interface IUserService
    {
        ResponseVM SignUp(RegisterUserVM user);
        ResponseVM SignIn(LoginUserVM user);
        ResponseVM GetUser(int? userId, string? email);
        ResponseVM ChangePassword(ChangePasswordVM user);
        ResponseVM DeleteUser(int userId);
    }
}
