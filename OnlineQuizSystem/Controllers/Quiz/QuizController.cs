using Application.DataTransferModels.ResponseModel;
using Domain.Models.Entities.Questions;
using Domain.Models.Entities.Options;
using Infrastructure.Services.Gemini;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Application.DataTransferModels.QuizViewModels;
using Application.Interfaces.Gemini;

namespace PresentationAPI.Controllers.Quiz
{
    [Route("api/quiz")]
    [ApiController]
    [Authorize]
    public class QuizController(IQuizService quizService) : ControllerBase
    {
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateQuiz([FromBody] QuizVM model)
        {

            var result = await quizService.GenerateQuizAsync(model);
            return Ok(result);
        }

        [HttpGet("quiz/{quizId:long}/questions")]
        public async Task<IActionResult> GetAllQuizQuestions(long quizId)
        {
            var result = await quizService.GetAllQuizQuestionsAsync(quizId);
            if (result.Data == null)
                return NotFound(result);
            return Ok(result);
        }

    }
}