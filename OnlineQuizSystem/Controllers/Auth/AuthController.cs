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
        [HttpPost("signup")]
        public IActionResult SignUp(RegisterUserVM user) // Requires Username, Email, Password
        { // user can sign up with username, email, and password
            ResponseVM response = ResponseVM.Instance;
            if (user == null)
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "User data is required";
                return BadRequest(response);
            }
            response = authService.SignUp(user);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // api/user/signin
        [HttpPost("signin")]
        public IActionResult SignIn(LoginUserVM user) // Requires Email, Password
        { // user can sign in with email and password
            ResponseVM response = ResponseVM.Instance;
            if (user == null)
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "User data is required";
                return BadRequest(response);
            }
            response = authService.SignIn(user);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/auth/signout
        [HttpPost("signout")]
        public IActionResult Logout(TokenRequestVM refreshToken) // Requires Refresh Token
        { // user can sign out by providing their refresh token, which will invalidate the token
            ResponseVM response = ResponseVM.Instance;
            if (refreshToken == null || string.IsNullOrEmpty(refreshToken.RefreshToken))
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Refresh token is required.";
                return BadRequest(response);
            }
            response = authService.SignOut(refreshToken);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/auth/verify-otp
        [HttpPost("verify-otp")]
        public IActionResult VerifyOTP([FromBody] VerifyOTPVM model) // Requires Email and OTP
        { // after correctly signing up, user needs to verify their email with OTP. by default, users status IsActive is false
            ResponseVM response = ResponseVM.Instance;
            if (string.IsNullOrEmpty(model.Email))
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Email is required to verify OTP.";
                return BadRequest(response);
            }
            response = authService.VerifyOTP(model.Email, model.OTP);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/auth/resend-otp
        [HttpPost("resend-otp")]
        public IActionResult ResendOTP([FromBody] EmailVM model) // Requires Email to resend OTP
        { // user can resend OTP to their email if they didn't receive it during signup
            ResponseVM response = ResponseVM.Instance;
            if (string.IsNullOrEmpty(model.Email))
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Email is required to resend OTP.";
                return BadRequest(response);
            }
            response = authService.ResendOTP(model.Email);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/auth/forgot-password
        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword([FromBody] EmailVM model) // Requires Email to send OTP for password reset
        { // user can reset their password by providing their email, which will send an OTP to that email
            ResponseVM response = ResponseVM.Instance;
            if (string.IsNullOrEmpty(model.Email))
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Email is required to reset the password.";
                return BadRequest(response);
            }
            response = authService.ResendOTP(model.Email, "forgot-password");
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/auth/reset-password
        [HttpPost("reset-password")]
        public IActionResult ResetPassword(ResetPasswordVM user) // Requires Email, Password and OTP
        { // After forgot-password, reset-password api will be called. Which will reset the user's password
            ResponseVM response = ResponseVM.Instance;
            if (user == null || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Email, OTP, and New Password are required to reset the password.";
                return BadRequest(response);
            }
            response = authService.ResetPassword(user.Email, user.Password);
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
            ResponseVM response = ResponseVM.Instance;
            if (request == null || string.IsNullOrEmpty(request.RefreshToken))
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "Refresh token is required.";
                return BadRequest(response);
            }
            response = authService.RefreshToken(request);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }
    }
}
