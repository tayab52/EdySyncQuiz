using Application.DataTransferModels.ResponseModel;
using Application.DataTransferModels.QuizViewModels;
using Application.Interfaces.Gemini; // Keep interface reference
using Microsoft.AspNetCore.Mvc;

namespace PresentationAPI.Controllers.Quiz
{
    [Route("mcp/quiz")]
    [ApiController]
    public class GenerateQuizController : ControllerBase
    {
        private readonly IQuizService _quizService;

        public GenerateQuizController(IQuizService quizService)
        {
            _quizService = quizService;
        }
        [HttpGet("generateQuiz")]
        public IActionResult GenerateQuiz([FromQuery] QuizVM model, [FromQuery] long? userId)
        {
            var response = new ResponseVM();

            model.Topic = string.IsNullOrWhiteSpace(model.Topic) ? string.Empty : model.Topic.Trim();
            model.SubTopic = string.IsNullOrWhiteSpace(model.SubTopic) ? string.Empty : model.SubTopic.Trim();

            try
            {
                ResponseVM result;
                if (userId.HasValue)
                {
                    // Use the MCP/no-auth path with explicit userId
                    result = _quizService.GenerateQuizForUser(model, userId.Value);
                }
                else
                {
                    // Normal path (uses TokenService/User from auth)
                    result = _quizService.GenerateQuiz(model);
                }

                if (result == null || result.Data == null)
                {
                    response.StatusCode = 204;
                    response.ResponseMessage = "No questions generated.";
                    return Ok(response);
                }

                response.StatusCode = 200;
                response.ResponseMessage = "Quiz generated successfully.";
                response.Data = result.Data;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error generating quiz: {ex.Message}";
            }

            return Ok(response);
        }

        [HttpGet("generate")]
        public IActionResult GenerateQuiz([FromQuery] QuizVM model)
        {
            var response = new ResponseVM();

            model.Topic = string.IsNullOrWhiteSpace(model.Topic) ? string.Empty : model.Topic.Trim();
            model.SubTopic = string.IsNullOrWhiteSpace(model.SubTopic) ? string.Empty : model.SubTopic.Trim();

            try
            {
                var result = _quizService.GenerateQuiz(model);

                if (result.Data == null)
                {
                    response.StatusCode = 204;
                    response.ResponseMessage = "No questions generated.";
                    return Ok(response);
                }

                response.StatusCode = 200;
                response.ResponseMessage = "Quiz generated successfully.";
                response.Data = result.Data;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error generating quiz: {ex.Message}";
            }

            return Ok(response);
        }

        [HttpGet("{quizId:long}/questions")]
        public IActionResult GetAllQuestions(long quizId)
        {
            var response = new ResponseVM();

            try
            {
                var result = _quizService.GetAllQuizQuestions(quizId);

                if (result.Data == null)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "No questions found for this quiz.";
                    return Ok(response);
                }

                response.StatusCode = 200;
                response.ResponseMessage = "Questions retrieved successfully.";
                response.Data = result.Data;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error retrieving questions: {ex.Message}";
            }

            return Ok(response);
        }
        [HttpGet("quiz/{quizId:long}/questions/{questionNumber:int}")]
        public async Task<IActionResult> GetQuizQuestionsByNumber(long quizId, int questionNumber)
        {
            var response = ResponseVM.Instance;

            try
            {
                var result = await _quizService.GetQuizQuestionsByNumberAsync(quizId, questionNumber);

                if (result.Data == null)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "Question not found.";
                    return Ok(response);
                }

                response.StatusCode = 200;
                response.ResponseMessage = "Question retrieved successfully.";
                response.Data = result.Data;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error retrieving question: {ex.Message}";
            }

            return Ok(response);
        }

        [HttpPost("resultSubmitted")]
        public IActionResult ResultSubmitted([FromBody] ResultSubmittedVM model)
        {
            var response = ResponseVM.Instance;

            if (model == null)
            {
                response.StatusCode = 400;
                response.ErrorMessage = "Invalid result submission data.";
                return Ok(response);
            }

            try
            {
                var result = _quizService.ResultSubmitted(model);
                response.StatusCode = 200;
                response.ResponseMessage = "Result submitted successfully.";
                response.Data = result.Data;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error submitting result: {ex.Message}";
            }

            return Ok(response);
        }

        [HttpGet("Quizdetails")]
        public IActionResult GetQuizDetails(long quizId)
        {
            var response = ResponseVM.Instance;

            try
            {
                var result = _quizService.GetQuizDetails(quizId);

                if (result == null)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "Quiz not found or has no questions.";
                    return Ok(response);
                }

                response.StatusCode = 200;
                response.ResponseMessage = "Quiz details retrieved successfully.";
                response.Data = result.Data;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error retrieving quiz details: {ex.Message}";
            }

            return Ok(response);
        }
        [HttpGet("Quizhistory")]
        public IActionResult GetQuizHistory()
        {
            var response = ResponseVM.Instance;

            try
            {
                var result = _quizService.GetQuizHistory();

                if (result.Data == null)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "No quiz history found.";
                    return Ok(response);
                }

                response.StatusCode = 200;
                response.ResponseMessage = "Quiz history retrieved successfully.";
                response.Data = result.Data;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error retrieving quiz history: {ex.Message}";
            }

            return Ok(response);
        }
        [HttpPost("resultSubmittedForUser")]
        public IActionResult ResultSubmittedForUser([FromBody] ResultSubmittedVM model, [FromQuery] long? userId)
        {
            var response = ResponseVM.Instance;

            if (model == null)
            {
                response.StatusCode = 400;
                response.ErrorMessage = "Invalid result submission data.";
                return Ok(response);
            }

            if (!userId.HasValue)
            {
                response.StatusCode = 400;
                response.ErrorMessage = "Missing userId query parameter.";
                return Ok(response);
            }

            try
            {
                var result = _quizService.ResultSubmittedForUser(model, userId.Value);
                if (result == null || (result.StatusCode != 200 && result.StatusCode != 0 && result.Data == null))
                {
                    // propagate service response if it contains error info
                    response.StatusCode = result?.StatusCode ?? 500;
                    response.ErrorMessage = result?.ErrorMessage ?? "Failed to submit results for user.";
                    return Ok(response);
                }

                response.StatusCode = 200;
                response.ResponseMessage = "Result submitted successfully for user.";
                response.Data = result.Data;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error submitting result for user: {ex.Message}";
            }

            return Ok(response);
        }

    }
}
