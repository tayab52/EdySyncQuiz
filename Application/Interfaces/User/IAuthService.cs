using Application.DataTransferModels.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.User
{
    public interface IAuthService
    {
        Task<ResponseVM> SendOTP(string email);
        Task<ResponseVM> ResendOTP(string email);
        Task<ResponseVM> VerifyOTP(string email, long otp);
        Task<ResponseVM> ForgotPassword(string email);
    }
}
