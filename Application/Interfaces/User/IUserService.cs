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
        Task<ResponseVM> SignUpAsync(RegisterUserVM user);
        Task<ResponseVM> SignInAsync(LoginUserVM user);
        Task<ResponseVM> GetUserByIdAsync(int userId);
        Task<ResponseVM> ChangePasswordAsync(ChangePasswordVM user);
        Task<ResponseVM> DeleteUserAsync(int userId);
    }
}
