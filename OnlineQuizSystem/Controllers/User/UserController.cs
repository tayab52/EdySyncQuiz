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
        public async Task<IActionResult> SignUp(RegisterUserVM user) // Requires Username, Email, Password
        {
            if (user == null)
            {
                return BadRequest("User data is required.");
            }
            var response = await _userService.SignUpAsync(user);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("SignIn")]
        public async Task<IActionResult> SignIn(LoginUserVM user) // Requires Email, Password
        {
            if (user == null)
            {
                return BadRequest("User data is required.");
            }
            var response = await _userService.SignInAsync(user);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("Verify-OTP")]
        public async Task<IActionResult> VerifyOTP(UserDTO userDTO) // Requires Email and OTP
        {
            if (userDTO?.OTP == null  && !string.IsNullOrEmpty(userDTO.Email))
            {
                return BadRequest("Verification data is required.");
            }
            var response = await _authService.VerifyOTP(userDTO.Email, userDTO.OTP.Value);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("Resend-OTP")]
        public async Task<IActionResult> ResendOTP(UserDTO userDTO) // Requires Email
        {
            if (string.IsNullOrEmpty(userDTO.Email))
            {
                return BadRequest("Email is required to resend OTP.");
            }
            var response = await _authService.ResendOTP(userDTO.Email);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("Forgot-Password")]
        public async Task<IActionResult> ForgotPassword(UserDTO userDTO) // Requires Email to send OTP for password reset
        {
            if(userDTO == null || string.IsNullOrEmpty(userDTO.Email))
            {
                return BadRequest("New Password is required to reset the password.");
            }

            var response = await _authService.ResendOTP(userDTO.Email, "forgot-password");
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("Reset-Password")]
        public async Task<IActionResult> ResetPassword(UserDTO userDTO) // Requires Email, OTP, and New Password
        {
            if (userDTO == null || string.IsNullOrEmpty(userDTO.Email) || userDTO.OTP == null || string.IsNullOrEmpty(userDTO.Password))
            {
                return BadRequest("Email, OTP, and New Password are required to reset the password.");
            }
            var response = await _authService.ResetPasswordAsync(userDTO.Email, userDTO.OTP.Value, userDTO.Password);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("Change-Password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM changePasswordVM) // Requires Old Password and New Password
        {
            if (changePasswordVM == null || string.IsNullOrEmpty(changePasswordVM.OldPassword) || string.IsNullOrEmpty(changePasswordVM.NewPassword))
            {
                return BadRequest("Old Password and New Password are required to change the password.");
            }
            var response = await _userService.ChangePasswordAsync(changePasswordVM);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        [HttpPost("Delete-Account/{UserId}")]
        public async Task<IActionResult> DeleteAccount(int userId) // Requires User ID
        {
            if (userId <= 0)
            {
                return BadRequest("User ID is required to delete the account.");
            }
            var response = await _userService.DeleteUserAsync(userId);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }


        [Authorize]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(int userId) // Requires User ID, Valid Token
        {
            if (userId <= 0)
            {
                return BadRequest("User ID is required to retrieve user information.");
            }
            var result = await _userService.GetUserByIdAsync(userId);
            if (result.StatusCode == ResponseCode.Forbidden || result.StatusCode == ResponseCode.Unauthorized
                || result.StatusCode == ResponseCode.InternalServerError || result.StatusCode == ResponseCode.NotFound)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
