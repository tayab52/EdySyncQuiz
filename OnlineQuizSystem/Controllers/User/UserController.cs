using Application.Interfaces.User;
using Microsoft.AspNetCore.Mvc;
using Application.DataTransferModels.UserViewModels;
using CommonOperations.Constants;
using Microsoft.AspNetCore.Authorization;

namespace PresentationAPI.Controllers.User
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("SignUp")]
        public async Task<IActionResult> SignUp([FromBody] RegisterUserVM user)
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
        public async Task<IActionResult> SignIn([FromBody] LoginUserVM user)
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

        [Authorize]
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            var result = await _userService.GetUserByIdAsync(userId);
            if (result.StatusCode == ResponseCode.Forbidden || result.StatusCode == ResponseCode.Unauthorized
                || result.StatusCode == ResponseCode.InternalServerError || result.StatusCode == ResponseCode.NotFound)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
