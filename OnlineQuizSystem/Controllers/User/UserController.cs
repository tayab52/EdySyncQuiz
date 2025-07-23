using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;
using Application.Interfaces.Auth;
using Application.Interfaces.User;
using CommonOperations.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace PresentationAPI.Controllers.User
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(IUserService userService, IAuthService authService, IUserDetailsService userDetailsService) : Controller
    {
        // /api/user/signup
        [HttpPost("SignUp")]
        public IActionResult SignUp(RegisterUserVM user) // Requires Username, Email, Password
        { // user can sign up with username, email, and password
            if (user == null)
            {
                return BadRequest("User data is required.");
            }
            ResponseVM response = userService.SignUp(user);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // api/user/signin
        [HttpPost("SignIn")]
        public async Task<IActionResult> SignIn(LoginUserVM user) // Requires Email, Password
        { // user can sign in with email and password
            if (user == null)
            {
                return BadRequest("User data is required.");
            }
            var response = await userService.SignIn(user);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // api/user/verify-otp?email={email}&otp={otp}
        [HttpPost("Verify-OTP")]
        public IActionResult VerifyOTP([FromQuery] string email, [FromQuery] long otp) // Requires Email and OTP
        { // after correctly signing up, user needs to verify their email with OTP. by default, users status IsActive is false
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required.");
            }
            ResponseVM response = authService.VerifyOTP(email, otp);

            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }

        // api/user/resend-otp?email={email}
        [HttpPost("Resend-OTP")]
        public IActionResult ResendOTP([FromQuery] string email) // Requires Email
        { // user can resend OTP to their email for any reason, such as forgetting the OTP or not receiving it
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required to resend OTP.");
            }
            ResponseVM response = authService.ResendOTP(email);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // api/user/forgot-password?email={email}
        [HttpPost("Forgot-Password")]
        public IActionResult ForgotPassword([FromQuery] string email) // Requires Email to send OTP for password reset
        { // user can reset their password by providing their email, which will send an OTP to that email
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("New Password is required to reset the password.");
            }

            ResponseVM response = authService.ResendOTP(email, "forgot-password");
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/user/reset-password
        [HttpPost("Reset-Password")]
        public IActionResult ResetPassword(ResetPasswordVM user) // Requires Email, Password and OTP
        { // After forgot-password, reset-password api will be called. Which will reset the user's password
            if (user == null || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
            {
                return BadRequest("Email, OTP, and New Password are required to reset the password.");
            }
            ResponseVM response = authService.ResetPassword(user.Email, user.OTP, user.Password);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/user/change-password
        [Authorize]
        [HttpPost("Change-Password")]
        public IActionResult ChangePassword(ChangePasswordVM user) // Requires Email, Old Password and New Password
        {
            if (user == null || string.IsNullOrEmpty(user.OldPassword)
                || string.IsNullOrEmpty(user.NewPassword) || string.IsNullOrEmpty(user.Email))
            {
                return BadRequest("Email, Old Password and New Password are required to change the password.");
            }
            ResponseVM response = userService.ChangePassword(user);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/user/delete-account/{userID}
        [Authorize]
        [HttpPost("Delete-Account/{UserId}")]
        public IActionResult DeleteAccount(int userId) // Requires User ID, Valid Token
        { // User must be logged in, and can only delete his own account
            if (userId <= 0)
            {
                return BadRequest("User ID is required to delete the account.");
            }
            ResponseVM response = userService.DeleteUser(userId);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/user?userId={userId}&email={email@gmail.com} [Either of the one is required] 
        [Authorize]
        [HttpGet]
        public IActionResult GetUser([FromQuery] int? userId, [FromQuery] string? email) // Requires User ID or Email and a  Valid Token
        { // User must be logged in, and can only accesss their own account
            if (userId == null && string.IsNullOrWhiteSpace(email))
                return BadRequest("User ID or Email is required.");

            ResponseVM response;

            response = userService.GetUser(userId, email);

            if (response.StatusCode == ResponseCode.NotFound ||
                response.StatusCode == ResponseCode.Forbidden ||
                response.StatusCode == ResponseCode.Unauthorized ||
                response.StatusCode == ResponseCode.InternalServerError)
                return BadRequest(response);
            return Ok(response);
        }

        // /api/user/save-user-details/{userId}
        [Authorize]
        [HttpPost("Save-User-Details/{userID}")]
        public IActionResult SaveUserDetails(int userID, UserDetailsVM userDetails) // Requires User ID and User Details
        { // User must be logged in, and can only update their own account details
            if (userID <= 0 || userDetails == null)
            {
                return BadRequest("User ID and User Details are required to save user details.");
            }
            ResponseVM response = userDetailsService.SaveUserDetails(userID, userDetails);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }
    }
}
