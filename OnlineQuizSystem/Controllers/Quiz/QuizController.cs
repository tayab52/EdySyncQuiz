using Infrastructure.Context;
using Infrastructure.Services.Gemini;
using Infrastructure.Services.Token;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PresentationAPI.Controllers.Quiz
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class QuizController(ClientDBContext clientDBContext, TokenService tokenService, GeminiQuizService quizService) : Controller
    {
        [HttpGet("Generate-Quiz")]
        public async Task<IActionResult> GenerateQuiz()
        {
            string userID = tokenService.UserID;
            var user = clientDBContext.Users.Find(int.Parse(userID));
            if (user == null || string.IsNullOrWhiteSpace(user.Interests))
                return BadRequest("User or interests not found");

            var quizJson = await quizService.GenerateQuizFromInterestsAsync(user.Interests);
            return Ok(quizJson);
        }
    }
}
