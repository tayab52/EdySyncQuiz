using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;
using Application.Interfaces.User;
using Application.Mapppers;
using Infrastructure.Context;

namespace Infrastructure.Services.User
{
    public class UserDetailsService : IUserDetailsService
    {
        private readonly ClientDBContext _clientDBContext;

        public UserDetailsService(ClientDBContext clientDBContext)
        {
            this._clientDBContext = clientDBContext;
        }
        public ResponseVM GetUserDetails(int userId)
        {
            throw new NotImplementedException();
        }

        public ResponseVM SaveUserDetails(int userID, UserDetailsVM userDetails)
        {
            ResponseVM response = ResponseVM.Instance;
            var user = _clientDBContext.Users.FirstOrDefault(u => u.UserID == userID);
            if(user ==  null)
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
                user.Interests = userDetails.Interests.Select(i => new Domain.Models.Entities.Users.UserInterest
                {
                    InterestName = i
                }).ToList();
                _clientDBContext.Users.Update(user);
                _clientDBContext.SaveChanges();
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
