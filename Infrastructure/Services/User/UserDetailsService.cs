using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;
using Application.Interfaces.User;
using Application.Mappers;
using Infrastructure.Context;
using Infrastructure.Services.Token;

namespace Infrastructure.Services.User
{
    public class UserDetailsService(ClientDBContext clientDBContext, TokenService tokenService) : IUserDetailsService
    {
        public ResponseVM SaveUserDetails(UserDetailsVM userDetails)
        {
            ResponseVM response = ResponseVM.Instance;
            Guid userID = tokenService.UserID;
            if (userID != null)
            {
                var user = clientDBContext.Users.FirstOrDefault(u => u.UserID == userID);
                if (user == null)
                {
                    response.StatusCode = 404;
                    response.ResponseMessage = "User not found";
                    return response;
                }
                try
                {
                    user.DateOfBirth = userDetails.DateOfBirth;
                    user.Gender = userDetails.Gender;
                    user.Languages = userDetails.Languages;
                    user.Level = userDetails.Level;
                    user.Interests = userDetails.Interests;
                    user.IsDataSubmitted = true;
                    clientDBContext.Users.Update(user);
                    clientDBContext.SaveChanges();
                    response.StatusCode = 200;
                    response.ResponseMessage = "User Details Updated Successfully";
                    response.Data = user.ToUserDTO();
                    return response;
                }
                catch
                {
                    response.StatusCode = 400;
                    response.ErrorMessage = "Failed to Save Changes";
                    return response;
                }
            }
            response.StatusCode = 401;
            response.ResponseMessage = "Unauthorized: User ID is required";
            return response;
        }
    }
}
