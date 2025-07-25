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
    [Authorize]
    public class UserController(IUserService userService, IAuthService authService, IUserDetailsService userDetailsService) : Controller
    {
        // /api/user/change-password
        [HttpPost("Change-Password")]
        public IActionResult ChangePassword(ChangePasswordVM user) // Requires Old Password and New Password
        {
            if (user == null || string.IsNullOrEmpty(user.OldPassword)
                || string.IsNullOrEmpty(user.NewPassword))
            {
                return BadRequest("Old Password and New Password are required to change the password.");
            }
            ResponseVM response = userService.ChangePassword(user);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/user/delete-account/{userID}
        [HttpPost("Delete-Account")]
        public IActionResult DeleteAccount() // Requires Valid Token
        { // User must be logged in, and can only delete his own account
            ResponseVM response = userService.DeleteUser();
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/user 
        [HttpGet]
        public IActionResult GetUser() // Requires a Valid Token
        { // User must be logged in, and can only accesss their own account
            ResponseVM response;

            response = userService.GetUser();

            if (response.StatusCode == ResponseCode.NotFound ||
                response.StatusCode == ResponseCode.Forbidden ||
                response.StatusCode == ResponseCode.Unauthorized ||
                response.StatusCode == ResponseCode.InternalServerError)
                return BadRequest(response);
            return Ok(response);
        }

        // /api/user/save-user-details/{userId}
        [HttpPost("Save-User-Details")]
        public IActionResult SaveUserDetails(UserDetailsVM userDetails) // Requires User ID and User Details
        { // User must be logged in, and can only update their own account details
            ResponseVM response = userDetailsService.SaveUserDetails(userDetails);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/user/save-profile-image
        [HttpPost("Save-Profile-Image")]
        public async Task<IActionResult> SaveProfileImage([FromBody] string base64Image) // Requires Base64 Image String
        { // User must be logged in, and can only update their own profile image
            if (string.IsNullOrEmpty(base64Image))
            {
                return BadRequest("Base64 Image String is required to save profile image.");
            }
            ResponseVM response = await userService.SaveUserProfileImage(base64Image);
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }

        // /api/user/get-profile-image
        [HttpGet("Get-Profile-Image")]
        public IActionResult GetProfileImage() // Requires User ID and Valid Token
        { // User must be logged in, and can only access their own profile image
            ResponseVM response = userService.GetUserProfileImage();
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
            return BadRequest(response);
        }
    }
}
