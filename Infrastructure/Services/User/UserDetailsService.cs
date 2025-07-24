using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;
using Application.Interfaces.User;
using Application.Mappers;
using Infrastructure.Context;

namespace Infrastructure.Services.User
{
    public class UserDetailsService(ClientDBContext clientDBContext) : IUserDetailsService
    {
        public ResponseVM GetUserDetails(int userId)
        {
            throw new NotImplementedException();
        }

        public ResponseVM SaveUserDetails(int userID, UserDetailsVM userDetails)
        {
            ResponseVM response = ResponseVM.Instance;
            var user = clientDBContext.Users.FirstOrDefault(u => u.UserID == userID);
            if (user == null)
            {
                response.StatusCode = 404;
                response.ResponseMessage = "User not found";
                return response;
            }
            try
            {
                user.Age = userDetails.Age;
                user.Gender = userDetails.Gender;
                user.Language = userDetails.Language;
                user.Level = userDetails.Level;
                user.Interests = [.. userDetails.Interests.Select(i => new Domain.Models.Entities.Users.UserInterest
                {
                    InterestName = i
                })];
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
    }
}
