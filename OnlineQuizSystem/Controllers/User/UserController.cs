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
	public class UserController(IUserService userService, IUserDetailsService userDetailsService) : Controller
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
		[HttpDelete("Delete-Account")]
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
		public async Task<IActionResult> SaveProfileImage([FromBody] ProfileImageVM base64Image) // Requires Base64 Image String
		{ // User must be logged in, and can only update their own profile image
			if (string.IsNullOrEmpty(base64Image.Base64Image))
			{
				return BadRequest("Base64 Image String is required to save profile image.");
			}
			ResponseVM response = await userService.SaveUserProfileImage(base64Image.Base64Image);
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

		// /api/user/update-languages
		[HttpPatch("Update-Languages")]
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
			return response;
		}

		// /api/user/update-interests
		[HttpPatch("Update-Interests")]
		public IActionResult UpdateInterests([FromBody] InterestVM interests) // Requires Interests String
		{ // User must be logged in, and can only update their own interests
			if (string.IsNullOrEmpty(interests.Interest))
			{
				return BadRequest("Interests string is required to update interests.");
			}
			ResponseVM response = userService.UpdateUserInterests(interests.Interest);
			if (response.StatusCode == ResponseCode.Success)
			{
				return Ok(response);
			}
			return BadRequest(response);
		}

		// /api/user/update-level
		[HttpPatch("Update-Level")]
		public IActionResult UpdateLevel([FromBody] LevelVM level) // Requires level
		{ // User must be logged in, and can only update their own level
			ResponseVM response = userService.UpdateUserLevel(level.Level);
			if (response.StatusCode == ResponseCode.Success)
			{
				return Ok(response);
			}
			return BadRequest(response);
		}

		// /api/user/update-theme
		[HttpPatch("Update-Theme")]
		public IActionResult UpdateTheme([FromBody] ThemeVM theme) // Requires theme
		{ // User must be logged in, and can only update their own theme
			if (string.IsNullOrEmpty(theme.Theme))
			{
				return BadRequest("Theme is required to update theme.");
			}
			ResponseVM response = userService.UpdateTheme(theme.Theme);
			if (response.StatusCode == ResponseCode.Success)
			{
				return Ok(response);
			}
			return BadRequest(response);
		}

		[HttpPut("Update-User")]
		public IActionResult UpdateUser(UserDTO user) // Requires UserDTO
		{ // User must be logged in, and can only update their own account details. updates whole user details
			if (user == null)
			{
				return BadRequest("User data is required to update user.");
			}
			ResponseVM response = userService.UpdateUser(user);
			if (response.StatusCode == ResponseCode.Success)
			{
				return Ok(response);
			}
			return BadRequest(response);
		}
	}
}
