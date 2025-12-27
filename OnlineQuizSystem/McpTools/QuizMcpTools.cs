using System.ComponentModel;
using ModelContextProtocol.Server;
using Application.Interfaces.Gemini;
using Application.DataTransferModels.QuizViewModels;
using Application.DataTransferModels.ResponseModel;
using Microsoft.AspNetCore.Mvc;

[McpServerToolType]
public static class QuizMcpTools
{
    // Tool 1: generate_quiz
    [McpServerTool, Description("Generate a quiz based on topic/subtopic for optional userId.")]
    public static object GenerateQuiz(
        [Description("Topic")] string topic,
        [Description("Subtopic")] string? subTopic,
        [Description("Question count")] int? questionCount,
        [Description("User id (external MCP)")] long? userId,
        [FromServices] IQuizService quizService)
    {
        var vm = new QuizVM { Topic = topic, SubTopic = subTopic, QuestionCount = questionCount };
        ResponseVM res = userId.HasValue
            ? quizService.GenerateQuizForUser(vm, userId.Value)
            : quizService.GenerateQuiz(vm);

        if (res == null || res.Data == null)
        {
            return new { statusCode = res?.StatusCode ?? 500, errorMessage = res?.ErrorMessage ?? "Generation failed" };
        }
        return new { statusCode = 200, data = res.Data };
    }

    // Tool 2: submit_quiz_result_for_user
    [McpServerTool, Description("Submit quiz results for a specific user.")]
    public static object SubmitQuizResultForUser(
        [Description("Quiz id")] long quizId,
        [Description("Correct questions")] int noOfCorrectQuestions,
        [Description("Incorrect questions")] int noOfIncorrectQuestions,
        [Description("Total score")] int totalScore,
        [Description("Obtained score")] int obtainedScore,
        [Description("Correct question ids")] long[] correctQuestionIds,
        [Description("User id (external MCP)")] long userId,
        [FromServices] IQuizService quizService)
    {
        var model = new ResultSubmittedVM
        {
            QuizID = quizId,
            NoOfCorrectQuestions = noOfCorrectQuestions,
            NoOfIncorrectQuestions = noOfIncorrectQuestions,
            TotalScore = totalScore,
            ObtainedScore = obtainedScore,
            CorrectQuestionIds = correctQuestionIds?.ToList() ?? new List<long>()
        };
        var res = quizService.ResultSubmittedForUser(model, userId);
        if (res == null || res.StatusCode != 200)
        {
            return new { statusCode = res?.StatusCode ?? 500, errorMessage = res?.ErrorMessage ?? "Submit failed" };
        }
        return new { statusCode = 200, saved = true };
    }

    // Tool 3: get_quiz_details
    [McpServerTool, Description("Retrieve full quiz details, optionally scoped to a user.")]
    public static object GetQuizDetails(
        [Description("Quiz id")] long quizId,
        [Description("User id (external MCP)")] long? userId,
        [FromServices] IQuizService quizService)
    {
        var res = userId.HasValue
            ? quizService.GetQuizDetailsForUser(quizId, userId.Value)
            : quizService.GetQuizDetails(quizId);

        if (res == null || res.Data == null)
        {
            return new { statusCode = res?.StatusCode ?? 404, errorMessage = res?.ErrorMessage ?? "Not found" };
        }
        return new { statusCode = 200, data = res.Data };
    }

    // Tool 4: get_quiz_history
    [McpServerTool, Description("Retrieve quiz history for a user or default user.")]
    public static object GetQuizHistory(
        [Description("User id (external MCP)")] long? userId,
        [FromServices] IQuizService quizService)
    {
        var res = userId.HasValue
            ? quizService.GetQuizHistoryForUser(userId.Value)
            : quizService.GetQuizHistory();

        if (res == null || res.Data == null)
            return new { statusCode = res?.StatusCode ?? 404, errorMessage = res?.ErrorMessage ?? "No history" };

        return new { statusCode = 200, data = res.Data };
    }
}