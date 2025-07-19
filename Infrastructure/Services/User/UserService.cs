using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;
using Application.Interfaces.Auth;
using Application.Interfaces.User;
using Application.Mapppers;
using Azure;
using CommonOperations.Constants;
using CommonOperations.Encryption;
using CommonOperations.Methods;
using Infrastructure.Context;
using Infrastructure.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Infrastructure.Services.User
{
    public class UserService : IUserService
    {
        private readonly IAuthService _authService;
        private readonly ClientDBContext _clientDBContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(IHttpContextAccessor httpContextAccessor, IAuthService authService, ClientDBContext clientDBContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _authService = authService;
            _clientDBContext = clientDBContext;
        }

        public Task<ResponseVM> SignUp(RegisterUserVM user)
        {
            ResponseVM response = new ResponseVM();

            if (!string.IsNullOrWhiteSpace(user.Username)
                && !string.IsNullOrWhiteSpace(user.Email)
                && !string.IsNullOrWhiteSpace(user.Password))
            {
                var existingUser = _clientDBContext.Users.FirstOrDefault(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    response.StatusCode = ResponseCode.Conflict;
                    response.ErrorMessage = "Error Signing Up! Email already in use.";
                    return Task.FromResult(response);
                }

                if (!Methods.IsValidEmailFormat(user.Email))
                {
                    response.StatusCode = ResponseCode.BadRequest;
                    response.ErrorMessage = "Invalid Email Format";
                    return Task.FromResult(response);
                }

                var otpTask = _authService.SendOTP(user.Email);
                otpTask.Wait();
                response = otpTask.Result;

                if (response is not ResponseVM otpResponse || otpResponse.StatusCode != ResponseCode.Success)
                {
                    response.StatusCode = ResponseCode.BadRequest;
                    response.ErrorMessage = "Failed to send OTP. Please try again.";
                    return Task.FromResult(response);
                }

                try
                {
                    Domain.Models.Entities.Users.User userToSave = user.ToDomainModel();
                    userToSave.OTP = response.Data;
                    userToSave.OTPExpiry = DateTime.UtcNow.AddMinutes(60);
                    userToSave.Password = Encryption.EncryptPassword(user.Password);
                    var result = _clientDBContext.Users.Add(userToSave);
                    _clientDBContext.SaveChanges();
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
            return Task.FromResult(response);
        }

        public Task<ResponseVM> SignIn(LoginUserVM user)
        {
            var response = new ResponseVM();

            if (!string.IsNullOrWhiteSpace(user.Email) && !string.IsNullOrWhiteSpace(user.Password))
            {
                try
                {
                    var existingUser = _clientDBContext.Users        // Check User Password by Encrypting and Checking with the one is database
                        .FirstOrDefault(u => u.Email == user.Email && Encryption.EncryptPassword(user.Password) == u.Password);
                    if(existingUser == null)
                    {
                        response.StatusCode = ResponseCode.Unauthorized;
                        response.ErrorMessage = "User does not exist.";
                        return Task.FromResult(response);
                    }

                    if (existingUser.IsDeleted)
                    {
                        response.StatusCode = ResponseCode.Unauthorized;
                        response.ErrorMessage = "User account is deleted.";
                        return Task.FromResult(response);
                    }
                    if (!existingUser.IsActive)
                    {
                        response.StatusCode = ResponseCode.Unauthorized;
                        response.ErrorMessage = "User account is not active. Please verify your email.";
                        return Task.FromResult(response);
                    }

                    if (existingUser != null)
                    {
                        response.StatusCode = ResponseCode.Success;
                        response.ResponseMessage = "Login Successful";
                        string token = _authService.GenerateJWT(existingUser);
                        response.Data = token;
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
            return Task.FromResult(response);
        }

        public Task<ResponseVM> GetUserById(int userId)
        {
            try
            {
                var user = _httpContextAccessor.HttpContext?.User;
                var tokenUserId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (tokenUserId == null)
                {
                    return Task.FromResult(new ResponseVM
                    {
                        StatusCode = ResponseCode.Unauthorized,
                        ErrorMessage = "Token is missing the user ID."
                    });
                }

                if (tokenUserId != userId.ToString())
                {
                    return Task.FromResult(new ResponseVM
                    {
                        StatusCode = ResponseCode.Forbidden,
                        ErrorMessage = "You are not allowed to access this user's data."
                    });
                }

                var userEntity = _clientDBContext.Users
                    .Where(u => u.UserID == userId)
                    .FirstOrDefault();

                if (userEntity == null)
                {
                    return Task.FromResult(new ResponseVM
                    {
                        StatusCode = ResponseCode.NotFound,
                        ErrorMessage = "User not found."
                    });
                }

                var userDto = new UserDTO
                {
                    UserID = userEntity.UserID,
                    Username = userEntity.Username,
                    Email = userEntity.Email,
                    Role = userEntity.Role,
                };

                return Task.FromResult(new ResponseVM
                {
                    StatusCode = ResponseCode.Success,
                    ResponseMessage = "User fetched successfully.",
                    Data = userDto
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ResponseVM
                {
                    StatusCode = ResponseCode.InternalServerError,
                    ErrorMessage = "An error occurred while retrieving the user.",
                });
            }
        }

        public Task<ResponseVM> ChangePassword(ChangePasswordVM user)
        {
            ResponseVM response = new ResponseVM();
            if (user == null || user.UserID <= 0 || string.IsNullOrWhiteSpace(user.OldPassword))
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Invalid User ID or Password";
                return Task.FromResult(response);
            }
            var existingUser = _clientDBContext.Users.Find(user.UserID);
            if (existingUser == null)
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "User not found.";
                return Task.FromResult(response);
            }
            try
            {
                if (Encryption.EncryptPassword(user.OldPassword) != existingUser.Password)
                {
                    response.StatusCode = ResponseCode.Unauthorized;
                    response.ErrorMessage = "Old Password is incorrect.";
                    return Task.FromResult(response);
                }
                if (user.NewPassword != user.ConfirmNewPassword)
                {
                    response.StatusCode = ResponseCode.BadRequest;
                    response.ErrorMessage = "Passwords don't match.";
                    return Task.FromResult(response);
                }
                existingUser.Password = Encryption.EncryptPassword(user.NewPassword);
                _clientDBContext.Users.Update(existingUser);
                _clientDBContext.SaveChanges();
                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = "Password updated successfully.";
            }
            catch (Exception ex)
            {
                response.StatusCode = ResponseCode.InternalServerError;
                response.ErrorMessage = "Failed to update user: " + ex.Message;
            }
            return Task.FromResult(response);
        }

        public Task<ResponseVM> DeleteUser(int userId)
        {
            ResponseVM response = new ResponseVM();
            if (userId <= 0)
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Invalid User ID";
                return Task.FromResult(response);
            }
            try
            {
                var existingUser = _clientDBContext.Users.Find(userId);
                if (existingUser == null)
                {
                    response.StatusCode = ResponseCode.NotFound;
                    response.ErrorMessage = "User not found.";
                    return Task.FromResult(response);
                }
                existingUser.IsDeleted = true; 
                _clientDBContext.Users.Update(existingUser);
                _clientDBContext.SaveChanges();
                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = "User deleted successfully.";
            }
            catch (Exception ex)
            {
                response.StatusCode = ResponseCode.InternalServerError;
                response.ErrorMessage = "Failed to delete user: " + ex.Message;
            }
            return Task.FromResult(response);
        }
    }
}
