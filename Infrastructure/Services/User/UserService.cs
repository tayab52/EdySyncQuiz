using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;
using Application.Interfaces.Auth;
using Application.Interfaces.User;
using Application.Mapppers;
using Azure;
using CommonOperations.Constants;
using CommonOperations.Encryption;
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

namespace Infrastructure.Services.User
{
    public class UserService : IUserService
    {
        private readonly IAuthService _authService;
        private readonly ClientDBContext _clientDBContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(IHttpContextAccessor httpContextAccessor,IAuthService authService, ClientDBContext clientDBContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _authService = authService;
            _clientDBContext = clientDBContext;
        }

        public async Task<ResponseVM> SignUpAsync(RegisterUserVM user)
        {
            ResponseVM response = new ResponseVM();

            if (!string.IsNullOrWhiteSpace(user.Username) 
                && !string.IsNullOrWhiteSpace(user.Email) 
                && !string.IsNullOrWhiteSpace(user.Password))
            {
                var existingUser = await _clientDBContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
                if (existingUser != null)
                {
                    response.StatusCode = ResponseCode.Conflict;
                    response.ErrorMessage = "Error Signing Up! Email already in use.";
                    return response;
                }

                try
                {
                    user.Password = Encryption.EncryptPassword(user.Password);
                    var result = await _clientDBContext.Users.AddAsync(user.ToDomainModel());
                    await _clientDBContext.SaveChangesAsync();
                    response.StatusCode = ResponseCode.Success;
                    response.ResponseMessage = "User Created Successfully";
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

        public async Task<ResponseVM> SignInAsync(LoginUserVM user)
        {
            var response = new ResponseVM();

            if (!string.IsNullOrWhiteSpace(user.Email) && !string.IsNullOrWhiteSpace(user.Password))
            {
                try
                {
                    var existingUser = await _clientDBContext.Users        // Check User Password by Encrypting and Checking with the one is database
                        .FirstOrDefaultAsync(u => u.Email == user.Email && Encryption.EncryptPassword(user.Password) == u.Password);
                    
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
            return response;
        }

        public async Task<ResponseVM> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = _httpContextAccessor.HttpContext?.User;
                var tokenUserId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (tokenUserId == null)
                {
                    return new ResponseVM
                    {
                        StatusCode = ResponseCode.Unauthorized,
                        ErrorMessage = "Token is missing the user ID."
                    };
                }

                if (tokenUserId != userId.ToString())
                {
                    return new ResponseVM
                    {
                        StatusCode = ResponseCode.Forbidden,
                        ErrorMessage = "You are not allowed to access this user's data."
                    };
                }

                var userEntity = await _clientDBContext.Users
                    .Where(u => u.UserID == userId)
                    .FirstOrDefaultAsync();

                if (userEntity == null)
                {
                    return new ResponseVM
                    {
                        StatusCode = ResponseCode.NotFound,
                        ErrorMessage = "User not found."
                    };
                }

                var userDto = new UserDTO
                {
                    UserID = userEntity.UserID,
                    Username = userEntity.Username,
                    Email = userEntity.Email,
                    Role = userEntity.Role,
                };

                return new ResponseVM
                {
                    StatusCode = ResponseCode.Success,
                    ResponseMessage = "User fetched successfully.",
                    Data = userDto
                };
            }
            catch (Exception ex)
            {
                return new ResponseVM
                {
                    StatusCode = ResponseCode.InternalServerError,
                    ErrorMessage = "An error occurred while retrieving the user.",
                };
            }
        }
    }
}
