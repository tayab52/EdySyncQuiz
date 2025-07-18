using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferModels.UserViewModels
{
    public class LoginUserVM
    {
        public string Email { get; set; } = string.Empty; 
        public string Password { get; set; } = string.Empty;
        public string? Role { get; set; } = "User"; 
    }
}
