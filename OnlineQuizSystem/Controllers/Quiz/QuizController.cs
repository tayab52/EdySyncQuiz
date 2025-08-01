using Application.DataTransferModels.Quiz;
using Application.DataTransferModels.ResponseModel;
using CommonOperations.Constants;
using Domain.Models.Entities.Questions;
using Infrastructure.Context;
using Infrastructure.Services.Gemini;
using Infrastructure.Services.Token;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
            //Console.WriteLine("Raw Quiz JSON:\n" + quizJson);
            //var quizDTOs = JsonSerializer.Deserialize<List<QuizQuestionDTO>>(quizJson!.Trim().Trim('`')[4..].Trim());
            //Console.WriteLine("Quiz DTOs: " + JsonSerializer.Serialize(quizDTOs));
            //List<Question> questions = [.. quizDTOs!.Select(dto => new Question
            //{
            //    QuestionID = Guid.NewGuid(),
            //    QuestionText = dto.Question,
            //})];
            //appDBContext.Questions.AddRange(questions);
            //await _dbContext.SaveChangesAsync();
            return Ok(quizJson);
        }
    }
}
