using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.TokenVM;
using Application.DataTransferModels.UserViewModels;
using Application.Interfaces.Auth;
using Application.Interfaces.User;
using CommonOperations.Constants;
using Domain.Models.Entities.Token;
using Infrastructure.Context;
using Infrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PresentationAPI.Controllers.Auth
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController(IAuthService authService) : Controller
    {
        // /api/auth/signup
        [HttpPost("SignUp")]
        public IActionResult SignUp(RegisterUserVM user) // Requires Username, Email, Password
        { // user can sign up with username, email, and password
            if (user == null)
            {
                return BadRequest("User data is required.");
            }
            ResponseVM response = authService.SignUp(user);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // api/user/signin
        [HttpPost("SignIn")]
        public IActionResult SignIn(LoginUserVM user) // Requires Email, Password
        { // user can sign in with email and password
            if (user == null)
            {
                return BadRequest("User data is required.");
            }
            var response = authService.SignIn(user);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/auth/signout
        [HttpPost("SignOut")]
        public IActionResult Logout(TokenRequestVM refreshToken) // Requires Refresh Token
        { // user can sign out by providing their refresh token, which will invalidate the token
            if (refreshToken == null || string.IsNullOrEmpty(refreshToken.RefreshToken))
            {
                return BadRequest("Refresh token is required.");
            }
            ResponseVM response = authService.SignOut(refreshToken);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/auth/verify-otp
        [HttpPost("Verify-OTP")]
        public IActionResult VerifyOTP([FromBody] VerifyOTPVM model) // Requires Email and OTP
        { // after correctly signing up, user needs to verify their email with OTP. by default, users status IsActive is false
            if (string.IsNullOrEmpty(model.email))
            {
                return BadRequest("Email is required.");
            }
            ResponseVM response = authService.VerifyOTP(model.email, model.otp);

            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }

        // /api/auth/resend-otp
        [HttpPost("Resend-OTP")]
        public IActionResult ResendOTP([FromBody] EmailVM model) // Requires Email to resend OTP
        { // user can resend OTP to their email if they didn't receive it during signup
            if (string.IsNullOrEmpty(model.Email))
            {
                return BadRequest("Email is required to resend OTP.");
            }

            ResponseVM response = authService.ResendOTP(model.Email);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/auth/forgot-password
        [HttpPost("Forgot-Password")]
        public IActionResult ForgotPassword([FromBody] EmailVM model) // Requires Email to send OTP for password reset
        { // user can reset their password by providing their email, which will send an OTP to that email
            if (string.IsNullOrEmpty(model.Email))
            {
                return BadRequest("New Password is required to reset the password.");
            }

            ResponseVM response = authService.ResendOTP(model.Email, "forgot-password");
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/auth/reset-password
        [HttpPost("Reset-Password")]
        public IActionResult ResetPassword(ResetPasswordVM user) // Requires Email, Password and OTP
        { // After forgot-password, reset-password api will be called. Which will reset the user's password
            if (user == null || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
            {
                return BadRequest("Email, OTP, and New Password are required to reset the password.");
            }
            ResponseVM response = authService.ResetPassword(user.Email, user.Password);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/auth/refresh
        [HttpPost("Refresh")]
        public IActionResult Refresh(TokenRequestVM request) // Requires Refresh Token to generate new access token
        { // user can refresh their access token by providing their refresh token
            if (request == null || string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest("Refresh token is required.");
            }
            ResponseVM response = authService.RefreshToken(request);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }
    }
}
