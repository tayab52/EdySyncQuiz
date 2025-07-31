using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.UserViewModels;
using Application.Interfaces.Auth;
using Application.Interfaces.User;
using CommonOperations.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationAPI.Controllers.User
{
	[Route("api/user")]
	[ApiController]
	[Authorize]
	public class UserController(IUserService userService, IUserDetailsService userDetailsService) : Controller
	{
		// /api/user 
		[HttpGet]
		public IActionResult GetUser() // Requires a Valid Token
		{ // User must be logged in, and can only accesss their own account
			ResponseVM response;

			response = userService.GetUser();

			if (response.StatusCode == ResponseCode.NotFound ||
				response.StatusCode == ResponseCode.Unauthorized ||
				response.StatusCode == ResponseCode.InternalServerError)
			{
				response.StatusCode = ResponseCode.BadRequest;
				response.ErrorMessage = "Failed to retrieve user data. Please try again later.";
				return BadRequest(response);
            }
			return Ok(response);
		}

        // /api/user/get-profile-image
        [HttpGet("get-profile-image")]
        public IActionResult GetProfileImage() // Requires User ID and Valid Token
        { // User must be logged in, and can only access their own profile image
			ResponseVM response;
            response = userService.GetUserProfileImage();
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
			response.StatusCode = ResponseCode.BadRequest;
			response.ErrorMessage = "Failed to retrieve profile image. Please try again later.";
            return BadRequest(response);
        }

        // /api/user/change-password
        [HttpPost("change-password")]
        public IActionResult ChangePassword(ChangePasswordVM user) // Requires Old Password and New Password
		{
			ResponseVM response = ResponseVM.Instance;
            if (user == null || string.IsNullOrEmpty(user.OldPassword)
				|| string.IsNullOrEmpty(user.NewPassword))
			{
				response.StatusCode = ResponseCode.BadRequest;
				response.ErrorMessage = "Old Password and New Password are required to change the password.";
                return BadRequest(response);
			}
			response = userService.ChangePassword(user);
			if (response.StatusCode == ResponseCode.Success)
			{
				return Ok(response);
			}
			return BadRequest(response);
		}

		// /api/user/save-user-details
		[HttpPost("save-user-details")]
		public IActionResult SaveUserDetails(UserDetailsVM userDetails) // Requires User ID and User Details
		{ // User must be logged in, and can only update their own account details
			ResponseVM response = userDetailsService.SaveUserDetails(userDetails);
			if (response.StatusCode == ResponseCode.Success)
			{
				return Ok(response);
			}
			response.StatusCode = ResponseCode.BadRequest;	
			response.ErrorMessage = "Failed to save user details. Please ensure all required fields are filled out correctly.";
            return BadRequest(response);
		}

		// /api/user/save-profile-image
		[HttpPost("save-profile-image")]
		public async Task<IActionResult> SaveProfileImage([FromBody] ProfileImageVM base64Image) // Requires Base64 Image String
		{ // User must be logged in, and can only update their own profile image
			ResponseVM response = ResponseVM.Instance;
            if (string.IsNullOrEmpty(base64Image.Base64Image))
			{
				response.StatusCode = ResponseCode.BadRequest;
				response.ErrorMessage = "Base64 Image String is required to save profile image.";
                return BadRequest(response);
			}
			response = await userService.SaveUserProfileImage(base64Image.Base64Image);
			if (response.StatusCode == ResponseCode.Success)
			{
				return Ok(response);
			}
			return BadRequest(response);
		}

		// /api/user/update-languages
		[HttpPatch("update-languages")]
		public ResponseVM UpdateLanguages([FromBody] LanguageVM languages) // Requires Languages String
		{ // User must be logged in, and can only update their own languages
			ResponseVM response = ResponseVM.Instance;
            if (string.IsNullOrEmpty(languages.Language))
			{
				response.ErrorMessage = "Languages string is required to update languages.";
				response.StatusCode = ResponseCode.BadRequest;
                return response;
			}
            response = userService.UpdateUserLanguages(languages.Language);
			if (response.StatusCode == ResponseCode.Success)
			{
				return response;
			}
			response.StatusCode = ResponseCode.BadRequest;
			response.ErrorMessage = "Failed to update languages. Please ensure the languages string is valid.";
            return response;
		}

		// /api/user/update-interests
		[HttpPatch("update-interests")]
		public IActionResult UpdateInterests([FromBody] InterestVM interests) // Requires Interests String
		{ // User must be logged in, and can only update their own interests
			ResponseVM response = ResponseVM.Instance;
            if (string.IsNullOrEmpty(interests.Interest))
			{
				response.StatusCode = ResponseCode.BadRequest;
				response.ErrorMessage = "Interests string is required to update interests.";
                return BadRequest(response);
			}
			response = userService.UpdateUserInterests(interests.Interest);
			if (response.StatusCode == ResponseCode.Success)
			{
				return Ok(response);
			}
			response.StatusCode = ResponseCode.BadRequest;
			response.ErrorMessage = "Failed to update interests. Please ensure the interests string is valid.";
            return BadRequest(response);
		}

		// /api/user/update-level
		[HttpPatch("update-level")]
		public IActionResult UpdateLevel([FromBody] LevelVM level) // Requires level
		{ // User must be logged in, and can only update their own level
			ResponseVM response = userService.UpdateUserLevel(level.Level);
			if (response.StatusCode == ResponseCode.Success)
			{
				return Ok(response);
			}
			response.StatusCode = ResponseCode.BadRequest;
			response.ErrorMessage = "Failed to update level. Please ensure the level is valid.";
            return BadRequest(response);
		}

		// /api/user/update-theme
		[HttpPatch("update-theme")]
		public IActionResult UpdateTheme([FromBody] ThemeVM theme) // Requires theme
		{ // User must be logged in, and can only update their own theme
			ResponseVM response = ResponseVM.Instance;
            if (string.IsNullOrEmpty(theme.Theme))
			{
				response.StatusCode = ResponseCode.BadRequest;
				response.ErrorMessage = "Theme is required to update theme.";
                return BadRequest(response);
			}
			response = userService.UpdateTheme(theme.Theme);
			if (response.StatusCode == ResponseCode.Success)
			{
				return Ok(response);
			}
			response.StatusCode = ResponseCode.BadRequest;
			response.ErrorMessage = "Failed to update theme. Please ensure the theme is valid.";
            return BadRequest(response);
		}

		[HttpPut("update-user")]
		public IActionResult UpdateUser(UserDTO user) // Requires UserDTO
		{ // User must be logged in, and can only update their own account details. updates whole user details
			ResponseVM response = ResponseVM.Instance;
            if (user == null)
			{
				response.StatusCode = ResponseCode.BadRequest;
				response.ErrorMessage = "User data is required to update user.";
                return BadRequest("User data is required to update user.");
			}
			response = userService.UpdateUser(user);
			if (response.StatusCode == ResponseCode.Success)
			{
				return Ok(response);
			}
			response.StatusCode = ResponseCode.BadRequest;
			response.ErrorMessage = "Failed to update user. Please ensure all required fields are filled out correctly.";
            return BadRequest(response);
		}

        // /api/user/delete-account
        [HttpDelete("Delete-Account")]
        public IActionResult DeleteAccount() // Requires Valid Token
        { // User must be logged in, and can only delete his own account
            ResponseVM response = userService.DeleteUser();
            if (response.StatusCode == ResponseCode.Success)
            {
                return Ok(response);
            }
			response.StatusCode = ResponseCode.BadRequest;
			response.ErrorMessage = "Failed to delete account. Please try again later.";
            return BadRequest(response);
        }
    }
}
