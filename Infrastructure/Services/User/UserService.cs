using Amazon.S3;
using Amazon.S3.Model;
using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;
using Application.Interfaces.User;
using Application.Mappers;
using CommonOperations.Constants;
using CommonOperations.Encryption;
using CommonOperations.Methods;
using Dapper;
using Infrastructure.Context;
using Infrastructure.Services.Token;
using Infrastructure.Services.Wasabi;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Infrastructure.Services.User
{
    public class UserService : IUserService
    {
        private readonly AppDBContext _appDBContext;
        private readonly TokenService _tokenService;
        private readonly IConfiguration _config;
        private readonly IAmazonS3 _s3Client;
        private readonly WasabiService _wasabiService;

        public UserService(
            AppDBContext appDBContext,
            TokenService tokenService,
            IConfiguration config,
            IAmazonS3 s3Client,
            WasabiService wasabiService)
        {
            _appDBContext = appDBContext;
            _tokenService = tokenService;
            _config = config;
            _s3Client = s3Client;
            _wasabiService = wasabiService;
        }


        public ResponseVM GetUser()
        {
            ResponseVM response = ResponseVM.Instance;

            try
            {
                

                //if (string.IsNullOrEmpty(tokenEmail) && tokenUserId == null)
                //{
                //    response.StatusCode = ResponseCode.Unauthorized;
                //    response.ErrorMessage = "Invalid Token";
                //    return response;
                //}

                var parameters = new DynamicParameters();
               // parameters.Add("@Email", _tokenService.Email);
                parameters.Add("@UserID", _tokenService.UserID);

                var users = Methods.ExecuteStoredProceduresList("SP_GetUser", parameters);

                if (users == null || !users.Result.Any())
                {
                    response.StatusCode = ResponseCode.NotFound;
                    response.ErrorMessage = "User does not exist.";
                    return response;
                }

                //dynamic firstUser = users.Result.First();
                //var userJson = JsonSerializer.Serialize(firstUser);
                //var userEntity = JsonSerializer.Deserialize<Domain.Models.Entities.Users.User>(userJson)!;

                //UserDTO userDTO = UserMapper.MapToDTO(users.Result);

                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = "User fetched successfully.";
                response.Data = users;
            }
            catch (Exception e)
            {
                response.StatusCode = ResponseCode.InternalServerError;
                response.ErrorMessage = "An error occurred while retrieving the user." + e;
            }

            return response;
        }

        public ResponseVM ChangePassword(ChangePasswordVM user)
        {
            ResponseVM response = ResponseVM.Instance;
            string? email = _tokenService?.Email;
            if (email == null)
            {
                response.StatusCode = ResponseCode.Unauthorized;
                response.ErrorMessage = "Invalid Token";
                return response;
            }

            var existingUser = _appDBContext.Users.FirstOrDefault(u => u.Email == email);
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
                _appDBContext.Users.Update(existingUser);
                _appDBContext.SaveChanges();
                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = "Password updated successfully.";
                response.Data = new
                {
                    existingUser.UserID
                };
            }
            catch
            {
                response.StatusCode = ResponseCode.InternalServerError;
                response.ErrorMessage = "Failed to update user: ";
            }
            return response;
        }

        public ResponseVM UpdateUser(UserDTO user)
        {
            ResponseVM response = ResponseVM.Instance;
            try
            {
                long tokenUserId = _tokenService.UserID;
                if (tokenUserId == null)
                {
                    response.StatusCode = ResponseCode.Unauthorized;
                    response.ErrorMessage = "Invalid Token";
                    return response;
                }
                var existingUser = _appDBContext.Users.Find(tokenUserId);
                if (existingUser == null)
                {
                    response.StatusCode = ResponseCode.NotFound;
                    response.ErrorMessage = "User not found.";
                    return response;
                }
                existingUser.Username = user.Username!;
                existingUser.Email = user.Email!;
                existingUser.DateOfBirth = user.DateOfBirth;
                existingUser.Interests = user.Interests;
                existingUser.Languages = user.Languages;
                existingUser.Gender = user.Gender;
                existingUser.Level = user.Level;
                existingUser.Theme = user.Theme ?? "light";
                _appDBContext.Users.Update(existingUser);
                _appDBContext.SaveChanges();
                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = "User updated successfully.";
                response.Data = existingUser.ToUserDTO();
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = ResponseCode.InternalServerError;
                response.ErrorMessage = "Failed to update user: " + ex.Message;
                return response;
            }
        }

        public ResponseVM DeleteUser()
        {
            ResponseVM response = ResponseVM.Instance;
            try
            {
                long tokenUserId = _tokenService.UserID;
               
                if (tokenUserId == null)
                {
                    response.StatusCode = ResponseCode.Unauthorized;
                    response.ErrorMessage = "Invalid Token";
                    return response;
                }

                var existingUser = _appDBContext.Users.Find(tokenUserId);
                if (existingUser == null)
                {
                    response.StatusCode = ResponseCode.NotFound;
                    response.ErrorMessage = "User not found.";
                    return response;
                }
                existingUser.IsDeleted = true;
                _appDBContext.Users.Update(existingUser);
                _appDBContext.SaveChanges();
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

            string userId = _tokenService.UserID.ToString();
            string filename = $"users/{userId}/profilepic_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fileExtension}";

            try
            {
                await _wasabiService.UploadBase64ImageAsync(base64Image, filename);
            }
            catch
            {
                response.StatusCode = ResponseCode.InternalServerError;
                response.ErrorMessage = "Failed to upload image to Wasabi.";
                return response;
            }

            string signedUrl = _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _config["WasabiSettings:BucketName"],
                Key = filename,
                Expires = DateTime.UtcNow.AddSeconds(int.Parse(_config["WasabiSettings:URLExpirySeconds"]!))
            });

            var user = _appDBContext.Users
                .FirstOrDefault(s => s.UserID == _tokenService.UserID && s.IsDeleted == false);

            if (user == null)
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "User not found.";
                return response;
            }

            user.ProfileImage = signedUrl;
            user.ImageKey = filename;
            user.ExpiresAt = DateTime.UtcNow.AddSeconds(int.Parse(_config["WasabiSettings:URLExpirySeconds"]!));

            _appDBContext.Users.Update(user);
            await _appDBContext.SaveChangesAsync();

            response.StatusCode = ResponseCode.Success;
            response.ResponseMessage = "User profile image saved to Wasabi.";
            response.Data = user.ToUserDTO();
            return response;
        }

        public ResponseVM GetUserProfileImage()
        {
            ResponseVM response = ResponseVM.Instance;
            string userId = _tokenService.UserID.ToString();

            var user = _appDBContext.Users
                .FirstOrDefault(s => s.UserID.ToString() == userId && s.IsDeleted == false);

            if (user == null)
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "User not found.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(user.ImageKey))
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "User profile image not found.";
                return response;
            }

            if (user.ExpiresAt <= DateTime.UtcNow.AddMinutes(1)) // check if url has expired or about to expire
            {
                string newSignedUrl = _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
                {
                    BucketName = _config["WasabiSettings:BucketName"],
                    Key = user.ImageKey,
                    Expires = DateTime.UtcNow.AddSeconds(int.Parse(_config["WasabiSettings:URLExpirySeconds"]!))
                });

                user.ProfileImage = newSignedUrl;
                user.ExpiresAt = DateTime.UtcNow.AddSeconds(int.Parse(_config["WasabiSettings:URLExpirySeconds"]!));

                _appDBContext.Users.Update(user);
                _appDBContext.SaveChanges();
            }

            response.StatusCode = ResponseCode.Success;
            response.ResponseMessage = "User profile image retrieved successfully.";
            response.Data = new { ProfileImageUrl = user.ProfileImage };
            return response;
        }

        public ResponseVM UpdateUserLanguages(string languages)
        {
            ResponseVM response = ResponseVM.Instance;
            try
            {
                long tokenUserId = _tokenService.UserID;
                if (tokenUserId == null)
                {
                    response.StatusCode = ResponseCode.Unauthorized;
                    response.ErrorMessage = "Invalid Token";
                    return response;
                }
                var existingUser = _appDBContext.Users.Find(tokenUserId);
                if (existingUser == null)
                {
                    response.StatusCode = ResponseCode.NotFound;
                    response.ErrorMessage = "User not found.";
                    return response;
                }
                existingUser.Languages = languages;
                _appDBContext.Users.Update(existingUser);
                _appDBContext.SaveChanges();
                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = "Languages updated successfully.";
                response.Data = existingUser.ToUserDTO();
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = ResponseCode.InternalServerError;
                response.ErrorMessage = "Failed to update user: " + ex.Message;
                return response;
            }
        }

        public ResponseVM UpdateUserInterests(string interests)
        {
            ResponseVM response = ResponseVM.Instance;
            try
            {
                long tokenUserId = _tokenService.UserID;
                if (tokenUserId != null)
                {
                    var existingUser = _appDBContext.Users.Find(tokenUserId);
                    if (existingUser == null)
                    {
                        response.StatusCode = ResponseCode.NotFound;
                        response.ErrorMessage = "User not found.";
                        return response;
                    }
                    existingUser.Interests = interests;
                    _appDBContext.Users.Update(existingUser);
                    _appDBContext.SaveChanges();
                    response.StatusCode = ResponseCode.Success;
                    response.ResponseMessage = "Interests updated successfully.";
                    response.Data = existingUser.ToUserDTO();
                    return response;
                }
                response.StatusCode = ResponseCode.Unauthorized;
                response.ErrorMessage = "Invalid Token";
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = ResponseCode.InternalServerError;
                response.ErrorMessage = "Failed to update user: " + ex.Message;
                return response;
            }
        }

        public ResponseVM UpdateTheme(string theme)
        {
            ResponseVM response = ResponseVM.Instance;
            try
            {
                long tokenUserId = _tokenService.UserID;
                if (tokenUserId == null)
                {
                    response.StatusCode = ResponseCode.Unauthorized;
                    response.ErrorMessage = "Invalid Token";
                    return response;
                }
                var existingUser = _appDBContext.Users.Find(tokenUserId);
                if (existingUser == null)
                {
                    response.StatusCode = ResponseCode.NotFound;
                    response.ErrorMessage = "User not found.";
                    return response;
                }
                existingUser.Theme = theme;
                _appDBContext.Users.Update(existingUser);
                _appDBContext.SaveChanges();
                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = "Level updated successfully.";
                response.Data = existingUser.ToUserDTO();
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = ResponseCode.InternalServerError;
                response.ErrorMessage = "Failed to update user: " + ex.Message;
                return response;
            }
        }

        public ResponseVM UpdateUserLevel(int level)
        {
            ResponseVM response = ResponseVM.Instance;
            try
            {
                long tokenUserId = _tokenService.UserID;
                if (tokenUserId == null)
                {
                    response.StatusCode = ResponseCode.Unauthorized;
                    response.ErrorMessage = "Invalid Token";
                    return response;
                }
                var existingUser = _appDBContext.Users.Find(tokenUserId);
                if (existingUser == null)
                {
                    response.StatusCode = ResponseCode.NotFound;
                    response.ErrorMessage = "User not found.";
                    return response;
                }
                existingUser.Level = level;
                _appDBContext.Users.Update(existingUser);
                _appDBContext.SaveChanges();
                response.StatusCode = ResponseCode.Success;
                response.ResponseMessage = "Level updated successfully.";
                response.Data = existingUser.ToUserDTO();
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = ResponseCode.InternalServerError;
                response.ErrorMessage = "Failed to update user: " + ex.Message;
                return response;
            }
        }

        public async Task<ResponseVM> UpdateUserProfile(string? username, string? base64Image)
        {
            ResponseVM response = ResponseVM.Instance;

            var userId = _tokenService.UserID;

            var user = await _appDBContext.Users.FindAsync(userId);
            if (user == null)
            {
                response.StatusCode = ResponseCode.NotFound;
                response.ErrorMessage = "User not found.";
                return response;
            }

            if (!string.IsNullOrEmpty(username)) user.Username = username;
            if (!string.IsNullOrEmpty(base64Image))
            {
                response = await SaveUserProfileImage(base64Image);
                if (response.StatusCode != ResponseCode.Success) // image upload failed
                {
                    return response;
                }
                user.ProfileImage = response.Data?.ProfileImage!;
            }

            _appDBContext.Users.Update(user);
            await _appDBContext.SaveChangesAsync();

            response.StatusCode = ResponseCode.Success;
            response.ResponseMessage = "Profile updated successfully.";
            response.Data = user.ToUserDTO();
            return response;
        }
    }
}
