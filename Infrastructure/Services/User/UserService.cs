using Amazon.S3;
using Amazon.S3.Model;
using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;
using Application.Interfaces.Auth;
using Application.Interfaces.User;
using Application.Mappers;
using CommonOperations.Constants;
using CommonOperations.Encryption;
using CommonOperations.Methods;
using Dapper;
using Infrastructure.Context;
using Infrastructure.Services.Token;
using Infrastructure.Services.Wasabi;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text.Json;

namespace Infrastructure.Services.User
{
    public class UserService(IAuthService authService, ClientDBContext clientDBContext, TokenService tokenService, IConfiguration config, IAmazonS3 s3Client, WasabiService wasabiService) : IUserService
    {
        public ResponseVM SignUp(RegisterUserVM user)
        {
            ResponseVM response = ResponseVM.Instance;

            if (!string.IsNullOrWhiteSpace(user.Username)
                && !string.IsNullOrWhiteSpace(user.Email)
                && !string.IsNullOrWhiteSpace(user.Password))
            {
                var existingUser = clientDBContext.Users.FirstOrDefault(u => u.Email == user.Email);
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

                response = authService.SendOTP(user.Email);

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
                    var result = clientDBContext.Users.Add(userToSave);
                    clientDBContext.SaveChanges();
                    response.StatusCode = ResponseCode.Success;
                    response.ResponseMessage = "User Created Successfully";
                    response.Data = result.Entity.UserID;
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

        public async Task<ResponseVM> SignIn(LoginUserVM user)
        {
            ResponseVM response = ResponseVM.Instance;

            if (!string.IsNullOrWhiteSpace(user.Email) && !string.IsNullOrWhiteSpace(user.Password))
            {
                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Email", user.Email);
                    parameters.Add("@Password", Encryption.EncryptPassword(user.Password));
                    var users = await Methods.ExecuteStoredProceduresList("SP_GetUserDetails", parameters);

                    if (users == null || !users.Any())
                    {
                        response.StatusCode = ResponseCode.Unauthorized;
                        response.ErrorMessage = "User does not exist.";
                        return response;
                    }
                    dynamic firstUser = users.First();
                    var userJson = JsonSerializer.Serialize(firstUser);
                    var userEntity = JsonSerializer.Deserialize<Domain.Models.Entities.Users.User>(userJson)!;

                    if (firstUser.IsDeleted)
                    {
                        response.StatusCode = ResponseCode.Unauthorized;
                        response.ErrorMessage = "User account is deleted.";
                        return response;
                    }

                    if (!firstUser.IsActive)
                    {
                        response.StatusCode = ResponseCode.Unauthorized;
                        response.ErrorMessage = "User account is not active. Please verify your email.";
                        return response;
                    }

                    response.StatusCode = ResponseCode.Success;
                    response.ResponseMessage = "Login Successful";
                    string token = authService.GenerateJWT(userEntity);
                    UserDTO userDTO = UserMapper.MapToDTO(users);

                    response.Data = new
                    {
                        Token = token,
                        User = userDTO
                    };
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

        public ResponseVM GetUser(int? userId, string? email)
        {
            ResponseVM response = ResponseVM.Instance;

            try
            {
                string? tokenUserId = tokenService.UserID;
                string? tokenEmail = tokenService.Email;

                if (userId.HasValue && !string.IsNullOrWhiteSpace(email)) // if both are provided, both must match the token
                {
                    if (tokenUserId != userId.ToString() || !string.Equals(tokenEmail, email, StringComparison.OrdinalIgnoreCase))
                    {
                        response.StatusCode = ResponseCode.Unauthorized;
                        response.ErrorMessage = "You are not allowed to access this user's data.";
                        return response;
                    }
                }
                else if (userId.HasValue) // if only userId is provided
                {
                    if (tokenUserId != userId.ToString())
                    {
                        response.StatusCode = ResponseCode.Unauthorized;
                        response.ErrorMessage = "You are not allowed to access this user's data by ID.";
                        return response;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(email)) // if only email is provided
                {
                    if (!string.Equals(tokenEmail, email, StringComparison.OrdinalIgnoreCase))
                    {
                        response.StatusCode = ResponseCode.Unauthorized;
                        response.ErrorMessage = "You are not allowed to access this user's data by email.";
                        return response;
                    }
                }
                else
                {
                    response.StatusCode = ResponseCode.BadRequest;
                    response.ErrorMessage = "User ID or Email is required.";
                    return response;
                }

                var userEntity = clientDBContext.Users
                    .AsNoTracking()
                    .Include(u => u.Interests)
                    .Where(u => (userId.HasValue && u.UserID == userId) ||
                                (!string.IsNullOrWhiteSpace(email) && u.Email == email))
                    .FirstOrDefault();

                if (userEntity == null)
                {
                    response.StatusCode = ResponseCode.NotFound;
                    response.ErrorMessage = "User not found.";
                    return response;
                }
                UserDTO userDto = userEntity.ToUserDTO();
                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = "User fetched successfully.";
                response.Data = userDto;
            }
            catch
            {
                response.StatusCode = ResponseCode.InternalServerError;
                response.ErrorMessage = "An error occurred while retrieving the user.";
            }

            return response;
        }

        public ResponseVM ChangePassword(ChangePasswordVM user)
        {
            ResponseVM response = ResponseVM.Instance;
            var existingUser = clientDBContext.Users.FirstOrDefault(u => u.Email == user.Email);
            if (existingUser == null)
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "User not found.";
                return response;
            }
            try
            {
                if (Encryption.EncryptPassword(user.OldPassword) != existingUser.Password)
                {
                    response.StatusCode = ResponseCode.Unauthorized;
                    response.ErrorMessage = "Old Password is incorrect.";
                    return response;
                }
                existingUser.Password = Encryption.EncryptPassword(user.NewPassword);
                clientDBContext.Users.Update(existingUser);
                clientDBContext.SaveChanges();
                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = "Password updated successfully.";
            }
            catch
            {
                response.StatusCode = ResponseCode.InternalServerError;
                response.ErrorMessage = "Failed to update user: ";
            }
            return response;
        }

        public ResponseVM DeleteUser(int userId)
        {
            ResponseVM response = ResponseVM.Instance;
            if (userId <= 0)
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Invalid User ID";
                return response;
            }
            try
            {
                string? tokenUserId = tokenService.UserID;
                string? tokenEmail = tokenService.Email;

                var userEntity = clientDBContext.Users
                    .Where(u => u.UserID == userId)
                    .FirstOrDefault();

                if (userEntity == null)
                {
                    response.StatusCode = ResponseCode.NotFound;
                    response.ErrorMessage = "User not found.";
                    return response;
                }

                var existingUser = clientDBContext.Users.Find(userId);
                if (existingUser == null)
                {
                    response.StatusCode = ResponseCode.NotFound;
                    response.ErrorMessage = "User not found.";
                    return response;
                }
                existingUser.IsDeleted = true;
                clientDBContext.Users.Update(existingUser);
                clientDBContext.SaveChanges();
                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = "User deleted successfully.";
            }
            catch (Exception ex)
            {
                response.StatusCode = ResponseCode.InternalServerError;
                response.ErrorMessage = "Failed to delete user: " + ex.Message;
            }
            return response;
        }

        public async Task<ResponseVM> SaveUserProfileImage(string base64Image)
        {
            ResponseVM response = ResponseVM.Instance;

            if (string.IsNullOrWhiteSpace(base64Image))
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Invalid image data.";
                return response;
            }

            string fileExtension = Methods.GetImageExtension(base64Image);
            if (fileExtension == "unknown")
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Unsupported image format.";
                return response;
            }

            string userId = tokenService.UserID.ToString();
            string filename = $"users/{userId}/profilepic_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fileExtension}";

            try
            {
                await wasabiService.UploadBase64ImageAsync(base64Image, filename);
            }
            catch
            {
                response.StatusCode = ResponseCode.InternalServerError;
                response.ErrorMessage = "Failed to upload image to Wasabi.";
                return response;
            }

            string signedUrl = s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = config["WasabiSettings:BucketName"],
                Key = filename,
                Expires = DateTime.UtcNow.AddSeconds(int.Parse(config["WasabiSettings:URLExpirySeconds"]!))
            });

            var user = clientDBContext.Users
                .FirstOrDefault(s => s.UserID.ToString() == tokenService.UserID && s.IsDeleted == false);

            if (user == null)
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "User not found.";
                return response;
            }

            user.ProfileImage = signedUrl;
            user.ImageKey = filename;
            user.ExpiresAt = DateTime.UtcNow.AddSeconds(int.Parse(config["WasabiSettings:URLExpirySeconds"]!));

            clientDBContext.Users.Update(user);
            await clientDBContext.SaveChangesAsync();

            response.StatusCode = ResponseCode.Success;
            response.ResponseMessage = "User profile image saved to Wasabi.";
            return response;
        }

        public ResponseVM GetUserProfileImage()
        {
            ResponseVM response = ResponseVM.Instance;
            string userId = tokenService.UserID.ToString();
            var user = clientDBContext.Users
                .FirstOrDefault(s => s.UserID.ToString() == userId.ToString() 
                                && s.IsDeleted == false);
            if (user == null)
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "User not found.";
                return response;
            }
            if (string.IsNullOrWhiteSpace(user.ProfileImage) || string.IsNullOrWhiteSpace(user.ImageKey))
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "User profile image not found.";
                return response;
            }
            string signedUrl = s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = config["WasabiSettings:BucketName"],
                Key = user.ImageKey,
                Expires = DateTime.UtcNow.AddSeconds(int.Parse(config["WasabiSettings:URLExpirySeconds"]!))
            });
            response.StatusCode = ResponseCode.Success;
            response.ResponseMessage = "User profile image retrieved successfully.";
            response.Data = new { ProfileImageUrl = signedUrl };
            return response;
        }
    }
}
