using Application.DataTransferModels.QuizViewModels;
using Application.DataTransferModels.ResponseModel;
using Application.Interfaces.Gemini;
using Infrastructure.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PresentationAPI.Controllers.Quiz
{
    [Route("mcp/quiz")]
    [ApiController]
    public class GenerateQuizController : ControllerBase
    {
        private readonly IQuizService _quizService;
        private readonly AppDBContext _dbContext;

        public GenerateQuizController(IQuizService quizService, AppDBContext dbContext)
        {
            _quizService = quizService;
            _dbContext = dbContext;
        }

        // Tool 1: generate_quiz
        [HttpGet("generateQuiz")]
        public IActionResult GenerateQuiz([FromQuery] QuizVM model, [FromQuery] long? userId)
        {
            var response = new ResponseVM();

            // Basic validation
            if (string.IsNullOrWhiteSpace(model.Topic))
            {
                response.StatusCode = 400;
                response.ErrorMessage = "Topic is required.";
                return Ok(response);
            }
            model.Topic = model.Topic.Trim();
            model.SubTopic = (model.SubTopic ?? string.Empty).Trim();

            try
            {
                ResponseVM result = userId.HasValue
                    ? _quizService.GenerateQuizForUser(model, userId.Value)
                    : _quizService.GenerateQuiz(model);

                if (result == null)
                {
                    response.StatusCode = 500;
                    response.ErrorMessage = "Quiz generation failed.";
                    return Ok(response);
                }

                if (result.Data == null)
                {
                    response.StatusCode = result.StatusCode != 0 ? result.StatusCode : 404;
                    response.ErrorMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? "No questions generated."
                        : result.ErrorMessage;
                    response.ResponseMessage = result.ResponseMessage;
                    return Ok(response);
                }

                response.StatusCode = 200;
                response.ResponseMessage = "Quiz generated successfully.";
                response.Data = result.Data;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error generating quiz: {ex.Message}";
                return Ok(response);
            }
        }

        // Tool 2: submit_quiz_result_for_user
        [HttpPost("resultSubmittedForUser")]
        public IActionResult ResultSubmittedForUser([FromBody] ResultSubmittedVM model, [FromQuery] long? userId)
        {
            var response = ResponseVM.Instance;

            // Validation
            if (model == null)
            {
                response.StatusCode = 400;
                response.ErrorMessage = "Invalid result submission data.";
                return Ok(response);
            }
            if (!userId.HasValue || userId.Value <= 0)
            {
                response.StatusCode = 400;
                response.ErrorMessage = "Missing or invalid userId query parameter.";
                return Ok(response);
            }
            if (model.QuizID <= 0)
            {
                response.StatusCode = 400;
                response.ErrorMessage = "quizId is required and must be positive.";
                return Ok(response);
            }
            if (model.NoOfCorrectQuestions < 0 || model.NoOfIncorrectQuestions < 0)
            {
                response.StatusCode = 400;
                response.ErrorMessage = "Question counts must be non-negative.";
                return Ok(response);
            }
            if (model.TotalScore < 0 || model.ObtainedScore < 0 || model.ObtainedScore > model.TotalScore)
            {
                response.StatusCode = 400;
                response.ErrorMessage = "Scores are invalid.";
                return Ok(response);
            }

            try
            {
                // MCP ownership: find quiz by ExternalUserId
                var quiz = _dbContext.Quizzes
                    .FirstOrDefault(q => q.ID == model.QuizID && q.ExternalUserId == userId.Value);

                if (quiz == null)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "Quiz not found.";
                    return Ok(response);
                }

                // Load quiz questions
                var questions = _dbContext.Questions.Where(q => q.QuizID == quiz.ID).ToList();
                if (questions.Count == 0)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "Quiz has no questions.";
                    return Ok(response);
                }

                // Validate counts do not exceed total questions
                var answeredCount = model.NoOfCorrectQuestions + model.NoOfIncorrectQuestions;
                if (answeredCount > questions.Count)
                {
                    response.StatusCode = 400;
                    response.ErrorMessage = $"Answered count ({answeredCount}) cannot exceed total quiz questions ({questions.Count}).";
                    return Ok(response);
                }

                // Validate correctQuestionIds membership and count coherence
                var quizQuestionIds = questions.Select(q => q.ID).ToHashSet();
                var providedIds = model.CorrectQuestionIds ?? new List<long>();
                var invalidIds = providedIds.Where(id => !quizQuestionIds.Contains(id)).Distinct().ToList();
                if (invalidIds.Count > 0)
                {
                    response.StatusCode = 400;
                    response.ErrorMessage = $"Invalid correctQuestionIds for quiz {model.QuizID}: {string.Join(", ", invalidIds)}.";
                    return Ok(response);
                }
                if (providedIds.Count != model.NoOfCorrectQuestions)
                {
                    response.StatusCode = 400;
                    response.ErrorMessage = "noOfCorrectQuestions must match the number of provided correctQuestionIds.";
                    return Ok(response);
                }

                // Update quiz
                quiz.IsCompleted = true;
                quiz.CorrectQuestionCount = model.NoOfCorrectQuestions;
                quiz.IncorrectQuestionCount = model.NoOfIncorrectQuestions;
                quiz.TotalScore = model.TotalScore;
                quiz.ObtainedScore = model.ObtainedScore;
                _dbContext.Quizzes.Update(quiz);

                // Update question IsCorrect flags strictly based on provided IDs
                foreach (var question in questions)
                {
                    question.IsCorrect = providedIds.Contains(question.ID);
                }
                _dbContext.Questions.UpdateRange(questions);

                _dbContext.SaveChanges();

                response.StatusCode = 200;
                response.ResponseMessage = "Results submitted successfully.";
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error: {ex.Message}";
                return Ok(response);
            }
        }
        // Tool 3: get_quiz_details
        [HttpGet("Quizdetails")]
        public IActionResult GetQuizDetails([FromQuery] long quizId, [FromQuery] long? userId)
        {
            var response = ResponseVM.Instance;
            if (quizId <= 0)
            {
                response.StatusCode = 400;
                response.ErrorMessage = "quizId is required and must be positive.";
                return Ok(response);
            }

            try
            {
                var result = userId.HasValue
                    ? _quizService.GetQuizDetailsForUser(quizId, userId.Value)
                    : _quizService.GetQuizDetails(quizId);

                if (result == null || result.Data == null)
                {
                    response.StatusCode = result?.StatusCode ?? 404;
                    response.ErrorMessage = result?.ErrorMessage ?? "Quiz not found or has no questions.";
                    return Ok(response);
                }

                response.StatusCode = 200;
                response.ResponseMessage = "Quiz details retrieved successfully.";
                response.Data = result.Data;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error retrieving quiz details: {ex.Message}";
                return Ok(response);
            }
        }

        [HttpGet("Quizhistory")]
        public IActionResult GetQuizHistory([FromQuery] long? userId)
        {
            var response = ResponseVM.Instance;
            try
            {
                var result = userId.HasValue
                    ? _quizService.GetQuizHistoryForUser(userId.Value)
                    : _quizService.GetQuizHistory();

                if (result == null || result.Data == null)
                {
                    response.StatusCode = result?.StatusCode ?? 404;
                    response.ErrorMessage = result?.ErrorMessage ?? "No quiz history found.";
                    return Ok(response);
                }

                response.StatusCode = 200;
                response.ResponseMessage = "Quiz history retrieved successfully.";
                response.Data = result.Data;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error retrieving quiz history: {ex.Message}";
                return Ok(response);
            }
        }
    }
}