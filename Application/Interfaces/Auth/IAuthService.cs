using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.TokenVM;
using Application.DataTransferModels.UserViewModels;

namespace Application.Interfaces.Auth
{
    public interface IAuthService
    {
        ResponseVM SignUp(RegisterUserVM user);
        ResponseVM SignIn(LoginUserVM user);
        ResponseVM SignOut(TokenRequestVM refreshToken);
        ResponseVM SendOTP(string email, string? subject = "Welcome To TopicTap");
        ResponseVM ResendOTP(string email, string? operation = "resend-otp");
        ResponseVM VerifyOTP(string email, long otp);
        ResponseVM SendEmail(string to, string subject, string body);
        ResponseVM ResetPassword(string email, string newPassword);
        string GenerateJWT(Domain.Models.Entities.Users.User user);
        AuthResult GenerateTokens(Domain.Models.Entities.Users.User user);
        ResponseVM RefreshToken(TokenRequestVM refreshTokenRequest);
    }
}
