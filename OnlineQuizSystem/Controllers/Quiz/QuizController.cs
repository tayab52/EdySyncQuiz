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
        [HttpGet("generate")]
        public async Task<IActionResult> GenerateQuiz([FromQuery] QuizVM model)
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
        [HttpGet("quiz/{quizId:long}/questions/{questionNumber:int}")]

        public async Task<IActionResult> GetQuizQuestionsByNumber(long quizId, int questionNumber)
        {
            var result = await quizService.GetQuizQuestionsByNumberAsync(quizId, questionNumber);
            if (result.Data == null)
                return NotFound(result);
            return Ok(result);

        }
        [HttpPost("resultSubmitted")]
        public IActionResult ResultSubmitted([FromBody] ResultSubmittedVM model)
        {
            if (model == null)
            {
                return BadRequest("Invalid result submission data.");
            }
            var result = quizService.ResultSubmitted(model);
            return Ok(result);
        }

        //[HttpPost("resultSubmitted")]

        //public async Task<IActionResult> ResultSubmitted([FromBody] ResultSubmittedVM model)
        //{
        //    if (model == null)
        //    {
        //        return BadRequest("Invalid result submission data.");
        //    }
        //    var result = await quizService.ResultSubmittedAsync(model);

        //    return Ok(result);

        //}
        [HttpGet("Quizhistory")]
        public IActionResult GetQuizHistory()
        {
            var result = quizService.GetQuizHistory();
            if (result.Data == null)
                return NotFound(result);
            return Ok(result);
        }


        //[HttpGet("Quizhistory")]
        //public async Task<IActionResult> GetQuizHistory()
        //{
        //    var result = await quizService.GetQuizHistoryAsync();
        //    if (result.Data == null)
        //        return NotFound(result);
        //    return Ok(result);
        //}

        //[HttpGet("Quizdetails")]
        //public async Task<IActionResult> GetQuizDetails(long quizId)
        //{
        //    var result = await quizService.GetQuizDetailsAsync(quizId);
        //    if (result == null)
        //        return NotFound("Quiz not found or has no questions.");
        //    return Ok(result);
        //}

        [HttpGet("Quizdetails")]
        public IActionResult GetQuizDetails(long quizId)
        {
            var result = quizService.GetQuizDetails(quizId);
            if (result == null)
                return NotFound("Quiz not found or has no questions.");
            return Ok(result);
        }
    }
}