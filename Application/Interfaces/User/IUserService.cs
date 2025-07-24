using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;

namespace Application.Interfaces.User
{
    public interface IUserService
    {
        ResponseVM SignUp(RegisterUserVM user);
        Task<ResponseVM> SignIn(LoginUserVM user);
        ResponseVM GetUser(int? userId, string? email);
        ResponseVM ChangePassword(ChangePasswordVM user);
        ResponseVM DeleteUser(int userId);
        Task<ResponseVM> SaveUserProfileImage(string base64Image);
        ResponseVM GetUserProfileImage();
    }
}
