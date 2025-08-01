using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.TokenVM;
using Application.DataTransferModels.UserViewModels;
using Application.Interfaces.Auth;
using Application.Mappers;
using CommonOperations.Constants;
using CommonOperations.Encryption;
using CommonOperations.Methods;
using Dapper;
using Domain.Models.Entities.Token;
using Infrastructure.Context;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services.Auth
{
    public class AuthService(AppDBContext appDBContext, IConfiguration config) : IAuthService
    {
        public ResponseVM SignUp(RegisterUserVM user)
        {
            ResponseVM response = ResponseVM.Instance;

            if (!string.IsNullOrWhiteSpace(user.Username)
                && !string.IsNullOrWhiteSpace(user.Email)
                && !string.IsNullOrWhiteSpace(user.Password))
            {
                var existingUser = appDBContext.Users.FirstOrDefault(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    response.StatusCode = ResponseCode.Conflict;
                    response.ErrorMessage = "Error Signing Up! Email already in use.";
                    return response;
                }

                if (!Methods.IsValidEmailFormat(user.Email))
                {
                    response.StatusCode = ResponseCode.BadRequest;
                    response.ErrorMessage = "Invalid Email Format";
                    return response;
                }

                response = SendOTP(user.Email);

                if (response.StatusCode != ResponseCode.Success)
                {
                    response.StatusCode = ResponseCode.BadRequest;
                    response.ErrorMessage = "Failed to send OTP. Please try again.";
                    return response;
                }

                try
                {
                    Domain.Models.Entities.Users.User userToSave = user.ToDomainModel();
                    userToSave.OTP = response.Data;
                    userToSave.OTPExpiry = DateTime.UtcNow.AddMinutes(60);
                    userToSave.Password = Encryption.EncryptPassword(user.Password);
                    var result = appDBContext.Users.Add(userToSave);
                    appDBContext.SaveChanges();
                    response.StatusCode = ResponseCode.Success;
                    response.ResponseMessage = "User Created Successfully";
                    response.Data = new
                    {
                        result.Entity.UserID
                    };
                }
                catch (Exception ex)
                {
                    response.StatusCode = ResponseCode.BadRequest;
                    response.ErrorMessage = "Failed to Create User: " + ex.Message;
                }
            }
            else
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Error Signing Up! Username, Email and Password are required!";
            }
            return response;
        }

        public ResponseVM SignIn(LoginUserVM user)
        {
            ResponseVM response = ResponseVM.Instance;

            if (!string.IsNullOrWhiteSpace(user.Email) && !string.IsNullOrWhiteSpace(user.Password))
            {
                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Email", user.Email);
                    parameters.Add("@Password", Encryption.EncryptPassword(user.Password));
                    var users = Methods.ExecuteStoredProceduresList("SP_GetUserDetails", parameters);

                    if (users == null || !users.Result.Any())
                    {
                        response.StatusCode = ResponseCode.Unauthorized;
                        response.ErrorMessage = "Invalid Credentials.";
                        return response;
                    }
                    dynamic firstUser = users.Result.First();
                    var userJson = JsonSerializer.Serialize(firstUser);
                    var userEntity = JsonSerializer.Deserialize<Domain.Models.Entities.Users.User>(userJson)!;

                    if (firstUser.IsDeleted)
                    {
                        response.StatusCode = ResponseCode.Success;
                        response.ErrorMessage = "User account is deleted.";
                        response.Data = new
                        {
                            isDeleted = userEntity.IsDeleted
                        };
                        return response;
                    }

                    if (!firstUser.IsActive)
                    {
                        response.StatusCode = ResponseCode.Success;
                        response.ErrorMessage = "User account is not active. Please verify your email.";
                        response.Data = new
                        {
                            isActive = userEntity.IsActive
                        };
                        return response;
                    }

                    response.StatusCode = ResponseCode.Success;
                    response.ResponseMessage = "Login Successful";
                    AuthResult tokens = GenerateTokens(userEntity);
                    UserDTO userDTO = UserMapper.MapToDTO(users.Result);
                    response.Data = UserMapper.FlattenUserWithToken(userDTO, tokens.AccessToken, tokens.RefreshToken);
                }
                catch (Exception ex)
                {
                    response.StatusCode = ResponseCode.BadRequest;
                    response.ErrorMessage = "Error Signing In!: " + ex.Message;
                }
            }
            else
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Error Signing In! Email and Password are required!";
            }

            return response;
        }

        public ResponseVM SignOut(TokenRequestVM refreshToken)
        {
            ResponseVM response = ResponseVM.Instance;
            var token = appDBContext.RefreshTokens.FirstOrDefault(x => x.Token == refreshToken.RefreshToken);
            if (token == null)
            {
                response.StatusCode = ResponseCode.Unauthorized;
                response.ErrorMessage = "Invalid refresh token.";
                return response;
            }
            token.IsRevoked = true;
            appDBContext.SaveChanges();
            response.StatusCode = ResponseCode.Success;
            response.ResponseMessage = "Logged out successfully.";
            return response;
        }

        public AuthResult GenerateTokens(Domain.Models.Entities.Users.User user)
        {
            var accessToken = GenerateJWT(user);

            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                UserID = user.UserID,
                ExpiresAt = DateTime.UtcNow.AddDays(15)
            };

            appDBContext.RefreshTokens.Add(refreshToken);
            appDBContext.SaveChanges();

            return new AuthResult
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token
            };
        }

        public string GenerateJWT(Domain.Models.Entities.Users.User user)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:Secret"]!));
            var encryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(config["JWT:EncryptionKey"]!));
            _ = Convert.FromBase64String(config["JWT:EncryptionKey"]!);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = config["JWT:ValidIssuer"],
                Audience = config["JWT:ValidAudience"],
                //Expires = DateTime.UtcNow.AddDays(7),
                Expires = DateTime.UtcNow.AddMinutes(15),
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

        public ResponseVM SendOTP(string email, string? subject = "Welcome To TopicTap")
        {
            ResponseVM response = ResponseVM.Instance;
            long OTP = Methods.GenerateOTP();
            string template = $"Your OTP to register account is: {OTP}";
            string emailSubject = subject!;
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

        public ResponseVM ResendOTP(string email, string? operation = "resend-otp")
        {
            ResponseVM response = ResponseVM.Instance;
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = appDBContext.Users.FirstOrDefault(u => u.Email.Contains(email, StringComparison.CurrentCultureIgnoreCase));

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
                appDBContext.SaveChanges();
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

        public ResponseVM VerifyOTP(string email, long otp)
        {
            ResponseVM response = ResponseVM.Instance;
            var user = appDBContext.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "User not found.";
                return response;
            }
            if (user.OTP == otp && user.OTPExpiry > DateTime.UtcNow)
            {
                if (!user.IsActive) user.IsActive = true;
                user.OTPExpiry = DateTime.MinValue;
                user.OTP = 0L;
                appDBContext.SaveChanges();
                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = "OTP verified successfully.";
                response.Data = new
                {
                    UserId = user.UserID
                };
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
            string smtpServer = config["SmtpSettings:Server"]!;
            int smtpPort = Convert.ToInt32(config["SmtpSettings:Port"])!;
            string smtpUsername = config["SmtpSettings:Username"]!;
            string smtpPassword = config["SmtpSettings:Password"]!;

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

                message.Body = new TextPart("plain")
                {
                    Text = body
                };

                smtpClient.Send(message);
                smtpClient.Disconnect(true);
            }
            return response;
        }

        public ResponseVM ResetPassword(string email, string newPassword)
        {
            ResponseVM response = ResponseVM.Instance;
            var user = appDBContext.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "User not found.";
                return response;
            }
            user.Password = Encryption.EncryptPassword(newPassword);
            try
            {
                appDBContext.SaveChanges();
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

        public ResponseVM RefreshToken(TokenRequestVM refreshTokenRequest)
        {
            ResponseVM response = ResponseVM.Instance;
            var refreshToken = appDBContext.RefreshTokens
                .FirstOrDefault(r => r.Token == refreshTokenRequest.RefreshToken);
            if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                response.StatusCode = ResponseCode.Unauthorized;
                response.ErrorMessage = "Invalid or expired refresh token.";
                return response;
            }
            var user = appDBContext.Users.Find(refreshToken.UserID);
            if (user == null)
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "User not found.";
                return response;
            }
            var newAccessToken = GenerateJWT(user);
            refreshToken.Token = Guid.NewGuid().ToString();
            refreshToken.ExpiresAt = DateTime.UtcNow.AddDays(15);
            appDBContext.SaveChanges();
            response.StatusCode = ResponseCode.Success;
            response.ResponseMessage = "Tokens refreshed successfully.";
            response.Data = new AuthResult
            {
                AccessToken = newAccessToken,
                RefreshToken = refreshToken.Token
            };
            return response;
        }
    }
}
