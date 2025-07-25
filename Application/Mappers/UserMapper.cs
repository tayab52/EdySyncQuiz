using Application.DataTransferModels.UserViewModels;
using Domain.Models.Entities.Token;
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
                Languages = user.Languages,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Level = user.Level,
                Interests = user.Interests
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
                Languages = first.Languages,
                Gender = first.Gender,
                DateOfBirth = first.DateOfBirth,
                Level = first.Level,
                Interests = first.Interests
            };
            return dto;
        }

        public static object FlattenUserWithToken(UserDTO dto, string accessToken, string refreshToken)
        {
            return new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                dto.UserID,
                dto.Username,
                dto.Email,
                dto.IsActive,
                dto.IsDeleted,
                dto.IsDataSubmitted,
                dto.Languages,
                dto.Gender,
                dto.DateOfBirth,
                dto.Level,
                dto.Interests
            };
        }
    }
}
