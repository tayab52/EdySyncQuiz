using Application.DataTransferModels.ResponseModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Models.Entities.Users;

namespace Application.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<ResponseVM> SendOTP(string email, string? subject = "Welcome To TopicTap");
        Task<ResponseVM> ResendOTP(string email, string? operation = "resend-otp");
        Task<ResponseVM> VerifyOTP(string email, long otp);
        string GenerateJWT(Domain.Models.Entities.Users.User user);
        Task<ResponseVM> SendEmail(string to, string subject, string body);
        Task<ResponseVM> ResetPassword(string email, long OTP,string newPassword);
    }
}
