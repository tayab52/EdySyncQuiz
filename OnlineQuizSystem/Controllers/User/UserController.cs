using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;
using Application.Interfaces.Auth;
using Application.Interfaces.User;
using CommonOperations.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult SignUp(RegisterUserVM user) // Requires Username, Email, Password
        {
            if (user == null)
            {
                return BadRequest("User data is required.");
            }
            ResponseVM response = _userService.SignUp(user);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("SignIn")]
        public IActionResult SignIn(LoginUserVM user) // Requires Email, Password
        {
            if (user == null)
            {
                return BadRequest("User data is required.");
            }
            ResponseVM response = _userService.SignIn(user);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("Verify-OTP")]
        public IActionResult VerifyOTP(UserDTO userDTO) // Requires Email and OTP
        {
            if (userDTO?.OTP == null && !string.IsNullOrEmpty(userDTO?.Email))
            {
                return BadRequest("Verification data is required.");
            }
            ResponseVM response = _authService.VerifyOTP(userDTO.Email, userDTO.OTP.Value);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("Resend-OTP")]
        public IActionResult ResendOTP(UserDTO userDTO) // Requires Email
        {
            if (string.IsNullOrEmpty(userDTO.Email))
            {
                return BadRequest("Email is required to resend OTP.");
            }
            ResponseVM response = _authService.ResendOTP(userDTO.Email);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("Forgot-Password")]
        public IActionResult ForgotPassword(UserDTO userDTO) // Requires Email to send OTP for password reset
        {
            if (userDTO == null || string.IsNullOrEmpty(userDTO.Email))
            {
                return BadRequest("New Password is required to reset the password.");
            }

            ResponseVM response = _authService.ResendOTP(userDTO.Email, "forgot-password");
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("Reset-Password")]
        public IActionResult ResetPassword(UserDTO userDTO) // Requires Email, OTP, and New Password
        {
            if (userDTO == null || string.IsNullOrEmpty(userDTO.Email) || userDTO.OTP == null || string.IsNullOrEmpty(userDTO.Password))
            {
                return BadRequest("Email, OTP, and New Password are required to reset the password.");
            }
            ResponseVM response = _authService.ResetPassword(userDTO.Email, userDTO.OTP.Value, userDTO.Password);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("Change-Password")]
        public IActionResult ChangePassword(ChangePasswordVM changePasswordVM) // Requires User ID, Old Password and New Password
        {
            if (changePasswordVM == null || string.IsNullOrEmpty(changePasswordVM.OldPassword) || string.IsNullOrEmpty(changePasswordVM.NewPassword))
            {
                return BadRequest("Old Password and New Password are required to change the password.");
            }
            ResponseVM response = _userService.ChangePassword(changePasswordVM);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("Delete-Account/{UserId}")]
        public IActionResult DeleteAccount(int userId) // Requires User ID
        {
            if (userId <= 0)
            {
                return BadRequest("User ID is required to delete the account.");
            }
            ResponseVM response = _userService.DeleteUser(userId);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }


        [Authorize]
        [HttpGet("{userId}")]
        public IActionResult GetUserById(int userId) // Requires User ID, Valid Token
        {
            if (userId <= 0)
            {
                return BadRequest("User ID is required to retrieve user information.");
            }
            var result = _userService.GetUserById(userId);
            if (result.StatusCode == ResponseCode.Forbidden || result.StatusCode == ResponseCode.Unauthorized
                || result.StatusCode == ResponseCode.InternalServerError || result.StatusCode == ResponseCode.NotFound)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
