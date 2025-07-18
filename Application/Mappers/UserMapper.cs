using Domain.Models.Entities.Users;
using Application.DataTransferModels.UserViewModels;

namespace Application.Mapppers
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
                Role = vm.Role ?? "User"
            };
        }

        public static User ToDomainModel(this LoginUserVM vm)
        {
            return new User
            {
                Email = vm.Email,
                Password = vm.Password,
                Role = vm.Role ?? "User"
            };
        }
    }
}
