using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;

namespace Application.Interfaces.User
{
    public interface IUserService
    {
        Task<ResponseVM> GetUser();
        ResponseVM ChangePassword(ChangePasswordVM user);
        ResponseVM DeleteUser();
        ResponseVM UpdateUser(UserDTO user);
        ResponseVM GetUserProfileImage();
        ResponseVM UpdateUserLanguages(string languages);
        ResponseVM UpdateUserInterests(string interests);
        ResponseVM UpdateUserLevel(int level);
        ResponseVM UpdateTheme(string theme);
        Task<ResponseVM> SaveUserProfileImage(string base64Image);
        Task<ResponseVM> UpdateUserProfile(string username, string? base64Image);
    }
}
