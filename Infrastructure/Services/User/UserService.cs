using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;
using Application.Interfaces.User;
using Infrastructure.Context;

namespace Infrastructure.Services.User
{
    public class UserService : IUserService
    {
        private readonly IAuthService _authService;
        private readonly ClientDBContext _clientDBContext;
        
        public UserService(IAuthService authService, ClientDBContext clientDBContext)
        {
            _authService = authService;
            _clientDBContext = clientDBContext;
        }

        public async Task<ResponseVM> SignUpAsync(RegisterUserVM user)
        {
            throw new NotImplementedException();
        }
        public async Task<ResponseVM> SignInAsync(LoginUserVM model)
        {
            throw new NotImplementedException();
        }

        public async Task<ResponseVM> SignOutAsync(string email)
        {
            throw new NotImplementedException();
        }
    }
}
