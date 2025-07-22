using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;
using Application.Interfaces.Auth;
using Application.Interfaces.User;
using Application.Mappers;
using CommonOperations.Constants;
using CommonOperations.Encryption;
using CommonOperations.Methods;
using Infrastructure.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Services.User
{
    public class UserService(IHttpContextAccessor httpContextAccessor, IAuthService authService, ClientDBContext clientDBContext) : IUserService
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

        public ResponseVM SignIn(LoginUserVM user)
        {
            ResponseVM response = ResponseVM.Instance;

            if (!string.IsNullOrWhiteSpace(user.Email) && !string.IsNullOrWhiteSpace(user.Password))
            {
                try
                {
                    var existingUser = clientDBContext.Users        // Check User Password by Encrypting and Checking with the one is database
                        .Include(u => u.Interests)
                        .FirstOrDefault(u => u.Email == user.Email && Encryption.EncryptPassword(user.Password) == u.Password);
                    if (existingUser == null)
                    {
                        response.StatusCode = ResponseCode.Unauthorized;
                        response.ErrorMessage = "User does not exist.";
                        return response;
                    }

                    if (existingUser.IsDeleted)
                    {
                        response.StatusCode = ResponseCode.Unauthorized;
                        response.ErrorMessage = "User account is deleted.";
                        return response;
                    }
                    if (!existingUser.IsActive)
                    {
                        response.StatusCode = ResponseCode.Unauthorized;
                        response.ErrorMessage = "User account is not active. Please verify your email.";
                        return response;
                    }

                    if (existingUser != null)
                    {
                        response.StatusCode = ResponseCode.Success;
                        response.ResponseMessage = "Login Successful";
                        string token = authService.GenerateJWT(existingUser);
                        UserDTO userDTO = existingUser.ToUserDTO();
                        response.Data = new
                        {
                            Token = token,
                            User = userDTO
                        };
                    }
                    else
                    {
                        response.StatusCode = ResponseCode.Unauthorized;
                        response.ErrorMessage = "Invalid Email or Password";
                    }
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
                var user = httpContextAccessor.HttpContext?.User;

                if (user == null)
                {
                    response.StatusCode = ResponseCode.BadRequest;
                    response.ErrorMessage = "User context not found.";
                    return response;
                }

                string? tokenUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                string? tokenEmail = user.FindFirst(ClaimTypes.Email)?.Value;

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
                var user = httpContextAccessor.HttpContext?.User;
                var tokenUserId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (tokenUserId == null)
                {
                    response.StatusCode = ResponseCode.BadRequest;
                    response.ErrorMessage = "Token is missing the user ID.";
                    return response;
                }

                if (tokenUserId != userId.ToString())
                {
                    response.StatusCode = ResponseCode.Unauthorized;
                    response.ErrorMessage = "You are not allowed to access this user's data.";
                    return response;
                }

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
    }
}
