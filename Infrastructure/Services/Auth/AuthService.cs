using Application.DataTransferModels.ResponseModel;
using Application.Interfaces.Auth;
using CommonOperations.Constants;
using CommonOperations.Encryption;
using CommonOperations.Methods;
using Infrastructure.Context;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly ClientDBContext _clientDBContext;
        private IConfiguration _config;

        public AuthService(ClientDBContext clientDBContext, IConfiguration config)
        {
            this._clientDBContext = clientDBContext;
            this._config = config;
        }

        public string GenerateJWT(Domain.Models.Entities.Users.User user)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Secret"]));
            var encryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(_config["JWT:EncryptionKey"]));
            var encryptionKeyBytes = Convert.FromBase64String(_config["JWT:EncryptionKey"]);

            var claims = new List<Claim>
            {
                new Claim("sub", user.UserID.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _config["JWT:ValidIssuer"],
                Audience = _config["JWT:ValidAudience"],
                SigningCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256),

                EncryptingCredentials = new EncryptingCredentials(
                    encryptionKey,
                    SecurityAlgorithms.Aes256KW,
                    SecurityAlgorithms.Aes256CbcHmacSha512)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(securityToken);
        }

        public ResponseVM ResendOTP(string email, string? operation = "resend-otp")
        {
            ResponseVM response = ResponseVM.Instance;
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = _clientDBContext.Users.FirstOrDefault(u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
            {
                response.ErrorMessage = "User not found.";
                response.StatusCode = ResponseCode.NotFound;
                return response;
            }
            if (user.IsActive && operation != "forgot-password")
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "User is already active. No need to resend OTP.";
                return response;
            }

            string subject = operation == "forgot-password" ? "Reset OTP for TopicTap account" : "Resend OTP for TopicTap";
            var otpResponse = SendOTP(email, subject);

            if (otpResponse.StatusCode != ResponseCode.Success)
                return otpResponse;

            if (otpResponse.Data is not long otp)
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Invalid OTP value returned.";
                return response;
            }

            user.OTP = otp;
            user.OTPExpiry = DateTime.UtcNow.AddMinutes(60);

            try
            {
                _clientDBContext.SaveChanges();
                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = operation == "forgot-password"
                                                    ? "OTP to reset account sent successfully"
                                                    : "OTP resent successfully.";
                return response;
            }
            catch
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Failed to update user OTP.";
                return response;
            }
        }

        public ResponseVM SendOTP(string email, string? subject = "Welcome To TopicTap")
        {
            ResponseVM response = ResponseVM.Instance;
            long OTP = Methods.GenerateOTP();
            string template = $"Your OTP to register account is: {OTP}";
            string emailSubject = subject;
            string emailBody = template;

            try
            {
                SendEmail(email, emailSubject, emailBody);
                response.StatusCode = ResponseCode.Success;
                response.Data = OTP;

            }
            catch (Exception ex)
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ResponseMessage = "Failed to send email: " + ex.Message;
            }
            return response;
        }

        public ResponseVM VerifyOTP(string email, long otp)
        {
            ResponseVM response = ResponseVM.Instance;
            var user = _clientDBContext.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "User not found.";
                return response;
            }
            if (user.OTP == otp && user.OTPExpiry > DateTime.UtcNow)
            {
                user.IsActive = true;
                _clientDBContext.SaveChanges();
                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = "OTP verified successfully.";
            }
            else
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Invalid or expired OTP.";
            }
            return response;
        }

        public ResponseVM SendEmail(string toEmail, string subject, string body)
        {
            string smtpServer = _config["SmtpSettings:Server"];
            int smtpPort = Convert.ToInt32(_config["SmtpSettings:Port"]);
            string smtpUsername = _config["SmtpSettings:Username"];
            string smtpPassword = _config["SmtpSettings:Password"];

            ResponseVM response = ResponseVM.Instance;

            using (var smtpClient = new MailKit.Net.Smtp.SmtpClient())
            {
                smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;

                smtpClient.Connect(smtpServer, smtpPort, SecureSocketOptions.SslOnConnect);

                smtpClient.Authenticate(smtpUsername, smtpPassword);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("TopicTap", smtpUsername));
                message.To.Add(new MailboxAddress("Recipient", toEmail));
                message.Subject = subject;


                //message.Body = new TextPart("html")
                //{
                //    Text = body
                //};
                message.Body = new TextPart("plain")
                {
                    Text = body
                };

                smtpClient.Send(message);
                smtpClient.Disconnect(true);
            }
            return response;
        }

        public ResponseVM ResetPassword(string email, long OTP, string newPassword)
        {
            ResponseVM response = ResponseVM.Instance;
            var user = _clientDBContext.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "User not found.";
                return response;
            }
            if (user.OTP != OTP || user.OTPExpiry < DateTime.UtcNow)
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Invalid or expired OTP.";
                return response;
            }
            user.Password = Encryption.EncryptPassword(newPassword);
            user.OTP = 0;
            try
            {
                _clientDBContext.SaveChanges();
                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = "Password reset successfully.";
            }
            catch (Exception ex)
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Failed to reset password: " + ex.Message;
            }
            return response;
        }
    }
}
