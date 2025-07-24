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
                IsDataSubmitted = user.IsDataSubmitted,
                Language = user.Language,
                Age = user.Age,
                Gender = user.Gender,
                Level = user.Level,
                Interests = [.. user.Interests.Select(i => new UserInterest
                {
                    InterestID = i.InterestID,
                    InterestName = i.InterestName,
                })]
            };
        }

        public static UserDTO MapToDTO(IEnumerable<dynamic> records)
        {
            var first = records.First();
            var dto = new UserDTO
            {
                UserID = first.UserID,
                Username = first.Username,
                Email = first.Email,
                IsActive = first.IsActive,
                IsDeleted = first.IsDeleted,
                IsDataSubmitted = first.IsDataSubmitted,
                Language = first.Language,
                Gender = first.Gender,
                Age = first.Age,
                Level = first.Level,
                Interests = new List<UserInterest>()
            };


            foreach (var r in records)
            {
                if(r.InterestID == null || r.InterestName == null)
                    continue;
                dto.Interests.Add(new UserInterest
                {
                    InterestID = r.InterestID,
                    InterestName = r.InterestName
                });
            }

            return dto;
        }
    }
}
