using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferModels.UserViewModels
{
    public class RegisterUserVM
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string? Role { get; set; } = "User"; 
    }
}
