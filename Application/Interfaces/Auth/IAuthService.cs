using Application.DataTransferModels.ResponseModel;

namespace Application.Interfaces.Auth
{
    public interface IAuthService
    {
        ResponseVM SendOTP(string email, string? subject = "Welcome To TopicTap");
        ResponseVM ResendOTP(string email, string? operation = "resend-otp");
        ResponseVM VerifyOTP(string email, long otp);
        string GenerateJWT(Domain.Models.Entities.Users.User user);
        ResponseVM SendEmail(string to, string subject, string body);
        ResponseVM ResetPassword(string email, long OTP, string newPassword);
    }
}
