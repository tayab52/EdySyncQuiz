using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferModels.UserViewModels
{
    public class VerifyOTPVM
    {
        public string email { get; set; }
        public long otp { get; set; }

    }
}
