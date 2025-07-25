using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;

namespace Application.Interfaces.User
{
    public interface IUserService
    {
        ResponseVM GetUser();
        ResponseVM ChangePassword(ChangePasswordVM user);
        ResponseVM DeleteUser();
        Task<ResponseVM> SaveUserProfileImage(string base64Image);
        ResponseVM GetUserProfileImage();
    }
}
