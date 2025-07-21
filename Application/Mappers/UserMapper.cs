using Application.DataTransferModels.UserViewModels;
using Domain.Models.Entities.Users;

namespace Application.Mappers
{
    public static class UserMapper
    {
        public static User ToDomainModel(this RegisterUserVM vm)
        {
            return new User
            {
                Username = vm.Username,
                Email = vm.Email,
                Password = vm.Password,
            };
        }

        public static UserDTO ToUserDTO(this User user)
        {
            return new UserDTO
            {
                UserID = user.UserID,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                IsDeleted = user.IsDeleted,
                Language = user.Language,
                Age = user.Age,
                Gender = user.Gender,
                Level = user.Level,
                Interests = [.. user.Interests.Select(i => new UserInterest
                {
                    InterestID = i.InterestID,
                    InterestName = i.InterestName
                })]
            };
        }
    }
}
