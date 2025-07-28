using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;

namespace Application.Interfaces.User
{
    public interface IUserService
    {
        ResponseVM GetUser();
        ResponseVM ChangePassword(ChangePasswordVM user);
        ResponseVM DeleteUser();
        ResponseVM UpdateUser(UserDTO user);
        Task<ResponseVM> SaveUserProfileImage(string base64Image);
        ResponseVM GetUserProfileImage();
        ResponseVM UpdateUserLanguages(string languages);
        ResponseVM UpdateUserInterests(string interests);
        ResponseVM UpdateUserLevel(int level);
        ResponseVM UpdateTheme(string theme);
    }
}
