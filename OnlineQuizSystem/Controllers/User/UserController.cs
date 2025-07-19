using Application.Interfaces.User;
using Microsoft.AspNetCore.Mvc;
using Application.DataTransferModels.UserViewModels;
using CommonOperations.Constants;
using Microsoft.AspNetCore.Authorization;
using Application.Interfaces.Auth;

namespace PresentationAPI.Controllers.User
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        public UserController(IUserService userService, IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        [HttpPost("SignUp")]
        public Task<IActionResult> SignUp(RegisterUserVM user) // Requires Username, Email, Password
        {
            if (user == null)
            {
                return Task.FromResult<IActionResult>(BadRequest("User data is required."));
            }
            var response =  _userService.SignUp(user);
            if (response.Result.StatusCode == ResponseCode.Success)
            {
                return Task.FromResult<IActionResult>(Ok(response));
            }
            return Task.FromResult<IActionResult>(BadRequest(response));
        }

        [HttpPost("SignIn")]
        public Task<IActionResult> SignIn(LoginUserVM user) // Requires Email, Password
        {
            if (user == null)
            {
                return Task.FromResult<IActionResult>(BadRequest("User data is required."));
            }
            var response = _userService.SignIn(user);
            if (response.Result.StatusCode == ResponseCode.Success)
            {
                return Task.FromResult<IActionResult>(Ok(response));
            }
            return Task.FromResult<IActionResult>(BadRequest(response));
        }

        [HttpPost("Verify-OTP")]
        public Task<IActionResult> VerifyOTP(UserDTO userDTO) // Requires Email and OTP
        {
            if (userDTO?.OTP == null  && !string.IsNullOrEmpty(userDTO.Email))
            {
                return Task.FromResult<IActionResult>(BadRequest("Verification data is required."));
            }
            var response = _authService.VerifyOTP(userDTO.Email, userDTO.OTP.Value);
            if (response.Result.StatusCode == ResponseCode.Success)
            {
                return Task.FromResult<IActionResult>(Ok(response));
            }
            return Task.FromResult<IActionResult>(BadRequest(response));
        }

        [HttpPost("Resend-OTP")]
        public Task<IActionResult> ResendOTP(UserDTO userDTO) // Requires Email
        {
            if (string.IsNullOrEmpty(userDTO.Email))
            {
                return Task.FromResult<IActionResult>(BadRequest("Email is required to resend OTP."));
            }
            var response = _authService.ResendOTP(userDTO.Email);
            if (response.Result.StatusCode == ResponseCode.Success)
            {
                return Task.FromResult<IActionResult>(Ok(response));
            }
            return Task.FromResult<IActionResult>(BadRequest(response));
        }

        [HttpPost("Forgot-Password")]
        public Task<IActionResult> ForgotPassword(UserDTO userDTO) // Requires Email to send OTP for password reset
        {
            if(userDTO == null || string.IsNullOrEmpty(userDTO.Email))
            {
                return Task.FromResult<IActionResult>(BadRequest("New Password is required to reset the password."));
            }

            var response = _authService.ResendOTP(userDTO.Email, "forgot-password");
            if (response.Result.StatusCode == ResponseCode.Success)
            {
                return Task.FromResult<IActionResult>(Ok(response));
            }
            return Task.FromResult<IActionResult>(BadRequest(response));
        }

        [HttpPost("Reset-Password")]
        public Task<IActionResult> ResetPassword(UserDTO userDTO) // Requires Email, OTP, and New Password
        {
            if (userDTO == null || string.IsNullOrEmpty(userDTO.Email) || userDTO.OTP == null || string.IsNullOrEmpty(userDTO.Password))
            {
                return Task.FromResult<IActionResult>(BadRequest("Email, OTP, and New Password are required to reset the password."));
            }
            var response = _authService.ResetPassword(userDTO.Email, userDTO.OTP.Value, userDTO.Password);
            if (response.Result.StatusCode == ResponseCode.Success)
            {
                return Task.FromResult<IActionResult>(Ok(response));
            }
            return Task.FromResult<IActionResult>(BadRequest(response));
        }

        [HttpPost("Change-Password")]
        public Task<IActionResult> ChangePassword(ChangePasswordVM changePasswordVM) // Requires User ID, Old Password and New Password
        {
            if (changePasswordVM == null || string.IsNullOrEmpty(changePasswordVM.OldPassword) || string.IsNullOrEmpty(changePasswordVM.NewPassword))
            {
                return Task.FromResult<IActionResult>(BadRequest("Old Password and New Password are required to change the password."));
            }
            var response = _userService.ChangePassword(changePasswordVM);
            if (response.Result.StatusCode == ResponseCode.Success)
            {
                return Task.FromResult<IActionResult>(Ok(response));
            }
            return Task.FromResult<IActionResult>(BadRequest(response));
        }

        [HttpPost("Delete-Account/{UserId}")]
        public Task<IActionResult> DeleteAccount(int userId) // Requires User ID
        {
            if (userId <= 0)
            {
                return Task.FromResult<IActionResult>(BadRequest("User ID is required to delete the account."));
            }
            var response = _userService.DeleteUser(userId);
            if (response.Result.StatusCode == ResponseCode.Success)
            {
                return Task.FromResult<IActionResult>(Ok(response));
            }
            return Task.FromResult<IActionResult>(BadRequest(response));
        }


        [Authorize]
        [HttpGet("{userId}")]
        public Task<IActionResult> GetUserById(int userId) // Requires User ID, Valid Token
        {
            if (userId <= 0)
            {
                return Task.FromResult<IActionResult>(BadRequest("User ID is required to retrieve user information."));
            }
            var result = _userService.GetUserById(userId);
            if (result.Result.StatusCode == ResponseCode.Forbidden || result.Result.StatusCode == ResponseCode.Unauthorized
                || result.Result.StatusCode == ResponseCode.InternalServerError || result.Result.StatusCode == ResponseCode.NotFound)
                return Task.FromResult<IActionResult>(BadRequest(result));
            return Task.FromResult<IActionResult>(Ok(result));
        }
    }
}
