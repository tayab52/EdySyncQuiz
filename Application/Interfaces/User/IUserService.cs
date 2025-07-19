using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.User
{
    public interface IUserService
    {
        Task<ResponseVM> SignUp(RegisterUserVM user);
        Task<ResponseVM> SignIn(LoginUserVM user);
        Task<ResponseVM> GetUserById(int userId);
        Task<ResponseVM> ChangePassword(ChangePasswordVM user);
        Task<ResponseVM> DeleteUser(int userId);
    }
}
