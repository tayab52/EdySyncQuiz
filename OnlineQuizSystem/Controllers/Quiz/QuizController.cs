using Infrastructure.Context;
using Infrastructure.Services.Gemini;
using Infrastructure.Services.Token;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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
            //var s = Stopwatch.StartNew();

            //var sw = Stopwatch.StartNew();
            Guid userID = tokenService.UserID;
            //sw.Stop();
            //Console.WriteLine($"Token service took {sw.ElapsedMilliseconds} ms");
            
            //var sw2 = Stopwatch.StartNew();
            var user = clientDBContext.Users.Find(userID);
            //sw2.Stop();
            //Console.WriteLine($"DB Operation took {sw2.ElapsedMilliseconds} ms");

            if (user == null || string.IsNullOrWhiteSpace(user.Interests))
                return BadRequest("User or interests not found");

            var quizJson = await quizService.GenerateQuizFromInterestsAsync(user.Interests, (int)user.Level!);
            //s.Stop();
            //Console.WriteLine($"Total Time took {s.ElapsedMilliseconds} ms");
            return Ok(quizJson);
        }
    }
}
