using Application.DataTransferModels.ResponseModel;
using CommonOperations.Constants;
using Infrastructure.Context;
using Infrastructure.Services.Gemini;
using Infrastructure.Services.Token;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationAPI.Controllers.Quiz
{
    [ApiController]
    [Route("api/quiz")]
    [Authorize]
    public class QuizController(AppDBContext appDBContext, TokenService tokenService, GeminiQuizService quizService) : Controller
    {
        [HttpGet("generate-quiz")]
        public async Task<IActionResult> GenerateQuiz()
        {
            ResponseVM response = ResponseVM.Instance;
            Guid userID = tokenService.UserID;
            var user = appDBContext.Users.Find(userID);
            if (user == null || string.IsNullOrWhiteSpace(user.Interests))
            {
                response.StatusCode = ResponseCode.BadRequest;
                response.ErrorMessage = "User or interests not found";
                return BadRequest(response);
            }
            var quizJson = await quizService.GenerateQuizFromInterestsAsync(user.Interests, (int)user.Level!);
            return Ok(quizJson);
        }
    }
}
