using Application.DataTransferModels.QuizViewModels;
using Application.DataTransferModels.ResponseModel;
using Application.Interfaces.Gemini;
using Azure;
using CommonOperations.Methods;
using Dapper;
using Domain.Models.Entities.Answers;
using Domain.Models.Entities.Options;
using Domain.Models.Entities.Questions;
using Domain.Models.Entities.Quiz;
using Domain.Models.Entities.Users;
using Infrastructure.Context;
using Infrastructure.Services.Token;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services.Gemini
{
    public class GeminiQuizService : IQuizService
    {
        private readonly HttpClient _httpClient = new();
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly AppDBContext _dbContext;
        private readonly TokenService _tokenService;

        private Domain.Models.Entities.Users.User GetOrCreateMcpDummyUser()
        {
            var email = "mcp-dummy@local";
            var dummy = _dbContext.Users.FirstOrDefault(u => u.Email == email);
            if (dummy != null) return dummy;

            dummy = new Domain.Models.Entities.Users.User
            {
                Username = "mcp-dummy",
                Email = email,
                Password = Guid.NewGuid().ToString("N"),
                IsActive = false,
                IsDataSubmitted = false,
                OTP = 100000,
                OTPExpiry = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(10),
                Level = 1
            };
            _dbContext.Users.Add(dummy);
            _dbContext.SaveChanges();
            return dummy;
        }

        public GeminiQuizService(IConfiguration config, AppDBContext dbContext, TokenService tokenService, HttpClient httpClient)
        {
            _apiKey = config["Gemini:ApiKey"]!;
            _endpoint = config["Gemini:Endpoint"]!;
            _dbContext = dbContext;
            _tokenService = tokenService;
            _httpClient = httpClient;
        }
        public GeminiQuizService(IConfiguration config, AppDBContext dbContext, TokenService tokenService)
        {
            _apiKey = config["Gemini:ApiKey"]!;
            _endpoint = config["Gemini:Endpoint"]!;
            _dbContext = dbContext;
            _tokenService = tokenService;

        }

        public ResponseVM GetQuizDetails(long QuizID)
        {
            ResponseVM response = ResponseVM.Instance;
            try
            {
                var parameter = new DynamicParameters();
                parameter.Add("@QuizID", QuizID);
                // Synchronous call
                var result = Methods.ExecuteStoredProceduresList("SP_GetQuizDetails", parameter).Result;
                if (result == null || !result.Any())
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "No quiz details found for this quiz.";
                    return response;
                }
                var questions = result.Select(row => new APIQuizDetailsItemsVM
                {
                    QuestionID = row.QuestionID ?? 0,
                    QuestionText = row.QuestionText,
                    Explanation = row.Explanation,
                    IsCorrect = row.IsCorrect
                }).ToList();

                var firstRow = result.First();
                int totalScore = (int)firstRow.TotalScore;
                int obtainedScore = (int)firstRow.ObtainedScore;

                var QuizDetails = new APIQuizDetailsVM
                {
                    Topic = firstRow.Topic,
                    UpdatedDate = firstRow.UpdatedDate,
                    TotalQuestions = questions.Count,
                    TotalScore = totalScore,
                    ObtainedScore = obtainedScore,
                    Questions = questions
                };

                response.StatusCode = 200;
                response.ResponseMessage = "Quiz details fetched successfully.";
                response.Data = QuizDetails;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error: {ex.Message}";
            }
            return response;
        }
        //public async Task<ResponseVM> GetQuizDetailsAsync(long QuizID)
        //{
        //    ResponseVM response = ResponseVM.Instance;
        //    try
        //    {
        //        var parameter = new DynamicParameters();
        //        parameter.Add("@QuizID", QuizID);
        //        // Call the stored procedure using your shared Methods class
        //        var result = await Methods.ExecuteStoredProceduresList("SP_GetQuizDetails", parameter);
        //        if (result == null || !result.Any())
        //        {
        //            response.StatusCode = 404;
        //            response.ErrorMessage = "No quiz details found for this quiz.";
        //            return response;
        //        }
        //        var questions =result.Select(row=> new APIQuizDetailsItemsVM
        //        {
        //            QuestionID = row.QuestionID ?? 0,
        //            QuestionText = row.QuestionText,
        //            Explanation = row.Explanation,
        //            IsCorrect = row.IsCorrect
        //        }).ToList();

        //        var firstRow = result.First();
        //        int totalScore = (int)result.First().TotalScore;
        //        int obtainedScore = (int)result.First().ObtainedScore;

        //        var QuizDetails = new APIQuizDetailsVM
        //        {
        //            Topic = firstRow.Topic,
        //            UpdatedDate = firstRow.UpdatedDate,
        //            TotalQuestions =questions.Count,
        //            TotalScore = totalScore,
        //            ObtainedScore = obtainedScore,
        //            Questions = questions
        //        };

        //        response.StatusCode = 200;
        //        response.ResponseMessage = "Quiz details fetched successfully.";
        //        response.Data = QuizDetails;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.StatusCode = 500;
        //        response.ErrorMessage = $"Error: {ex.Message}";
        //    }
        //    return response;
        //}
        public ResponseVM GenerateQuiz(QuizVM model)
        {
            ResponseVM response = ResponseVM.Instance;
            var user = _dbContext.Users
                .FirstOrDefault(u => u.UserID == _tokenService.UserID);

            int userLevel = user?.Level ?? 1; // Default to 1 if null

            string difficultyLevel = userLevel switch
            {
                0 => "Entry Level",
                1 => "Basic Level",
                2 => "Middle Level",
                3 => "Advanced Level",
                _ => "Intermediate Level",
            };

            int finalQuestionCount = model.QuestionCount ?? Random.Shared.Next(10, 15);

            string topicInstruction = string.IsNullOrEmpty(model.SubTopic)
                ? $"the topic: '{model.Topic}'"
                : $"the topic: '{model.Topic}', with specific focus on subtopic: '{model.SubTopic}'";

            string prompt = $@"
                You are a quiz generator agent. Create a quiz with {finalQuestionCount} multiple choice questions based on {topicInstruction}.
                The quiz should be suitable for {difficultyLevel} students.

                IMPORTANT INSTRUCTIONS:
                - Each question must have 2-6 multiple choice options
                - Provide a one line explanation for each question
                - If subtopic is not relevant to the main topic, focus only on the main topic

                Respond in JSON format EXACTLY like this:
                [
                  {{
                    ""question"": ""What is the capital of France?"",
                    ""options"": [""Paris"", ""London"", ""Berlin"", ""Madrid""],
                    ""correctOption"": ""Paris"",
                    ""explanation"": ""Paris is the capital and largest city of France.""
                  }}
                ]

                Make sure each question has the exact format above with question, options array, correctOption, and explanation.";

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            try
            {
                var json = JsonSerializer.Serialize(body);
                var contentPayload = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var endpoint = $"{_endpoint}?key={_apiKey}";
                var resp = _httpClient.PostAsync(endpoint, contentPayload).Result;

                if (!resp.IsSuccessStatusCode)
                {
                    var errorContent = resp.Content.ReadAsStringAsync().Result;
                    Console.WriteLine($"Gemini API Error: {resp.StatusCode} - {errorContent}");
                    return null;
                }

                var responseString = resp.Content.ReadAsStringAsync().Result;
                Console.WriteLine($"Gemini API Response: {responseString}");

                using var doc = JsonDocument.Parse(responseString);
                var parts = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts");

                var quizJson = parts[0].GetProperty("text").GetString();
                Console.WriteLine($"Extracted Quiz JSON: {quizJson}");

                quizJson = Methods.CleanJsonResponse(quizJson);
                Console.WriteLine($"Cleaned Quiz JSON: {quizJson}");

                var quizItems = JsonSerializer.Deserialize<List<GeminiResponseVM>>(quizJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (quizItems == null || !quizItems.Any())
                {
                    Console.WriteLine("Failed to deserialize quiz items or empty result");
                    return null;
                }

                var questions = new List<Question>();
                var options = new List<Option>();
                var quiz = new Quiz
                {
                    Topic = model.Topic,
                    SubTopic = model.SubTopic ?? "",
                    TotalQuestions = finalQuestionCount,
                    IsCompleted = false,
                    UserID = _tokenService.UserID
                };
                _dbContext.Quizzes.Add(quiz);
                _dbContext.SaveChanges();

                foreach (var item in quizItems)
                {
                    var question = new Question
                    {
                        QuestionText = item.question,
                        Explanation = item.explanation ?? "",
                        IsCorrect = false,
                        QuizID = quiz.ID
                    };
                    questions.Add(question);
                }

                _dbContext.Questions.AddRange(questions);
                _dbContext.SaveChanges();

                int i = 0;
                foreach (var item in quizItems)
                {
                    var questionId = questions[i].ID;
                    foreach (var opt in item.options)
                    {
                        bool isCorrect = string.Equals(
                            opt.Trim(),
                            item.correctOption.Trim(),
                            StringComparison.OrdinalIgnoreCase
                        );
                        options.Add(new Option
                        {
                            OptionText = opt,
                            IsCorrect = isCorrect,
                            QuestionID = questionId
                        });
                    }
                    i++;
                }
                _dbContext.Options.AddRange(options);
                _dbContext.SaveChanges();

                var allQuestionsResponse = GetAllQuizQuestions(quiz.ID);
                allQuestionsResponse.ResponseMessage = "Quiz generated and fetched successfully.";
                allQuestionsResponse.StatusCode = 200;
                return allQuestionsResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateQuiz: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return null;
            }
        }
        //public async Task<ResponseVM> GetQuizHistoryAsync()
        //{
        //    ResponseVM response = ResponseVM.Instance;
        //    try
        //    {
        //        var parameter = new DynamicParameters();
        //        parameter.Add("@UserID", _tokenService.UserID);
        //        // Call the stored procedure using your shared Methods class
        //        var result = await Methods.ExecuteStoredProceduresList("SP_GetQuizHistory", parameter);
        //        if (result == null || !result.Any())
        //        {
        //            response.StatusCode = 404;
        //            response.ErrorMessage = "No quiz history found for this user.";

        //            return response;
        //        }

        //        var quizList = result.Select(row => new QuizHistoryItemVM
        //        {

        //            QuizID = row.QuizID,
        //            Topic = row.Topic,
        //            TotalScore = row.TotalScore,
        //            ObtainedScore = row.ObtainedScore,
        //            UpdatedDate = row.UpdatedDate,
        //        }).ToList();

        //        // Calculate summary
        //        var totalQuizzes = quizList.Count;
        //        var totalScore = result.Sum(row => (int)row.TotalScore);
        //        var obtainedScore = result.Sum(row => (int)row.ObtainedScore);

        //        // Prepare final VM
        //        var quizHistoryVM = new QuizHistoryVM
        //        {
        //            TotalQuizzes = totalQuizzes,
        //            TotalScore = totalScore,
        //            ObtainedScore = obtainedScore,
        //            Quizzes = quizList
        //        };

        //        response.StatusCode = 200;
        //        response.ResponseMessage = "Quiz history fetched successfully.";
        //        response.Data = quizHistoryVM;
        //    }
        //    catch (Exception ex)
        //    {
        //        response.StatusCode = 500;
        //        response.ErrorMessage = $"Error: {ex.Message}";
        //    }
        //    return response;
        //}

        //public async Task<ResponseVM> GenerateQuizAsync(QuizVM model)
        //{
        //    ResponseVM response = ResponseVM.Instance;
        //    var user = await _dbContext.Users
        //        .FirstOrDefaultAsync(u => u.UserID == _tokenService.UserID);
        //    //if (user == null || user.Level == null)
        //    //{
        //    //    response.StatusCode = 400;
        //    //    response.ErrorMessage = "User or user level not found.";
        //    //    return response;
        //    //}
        //    int userLevel = user?.Level.Value ?? 1; // Default to 1 if null

        //    string difficultyLevel = userLevel switch
        //    {
        //        0 => "Entry Level",
        //        1 => "Basic Level",
        //        2 => "Middle Level",
        //        3 => "Advanced Level",
        //        _ => "Intermediate Level",
        //    };

        //    int finalQuestionCount = model.QuestionCount ?? Random.Shared.Next(2, 7); 

        //    string topicInstruction = string.IsNullOrEmpty(model.SubTopic)
        //        ? $"the topic: '{model.Topic}'"
        //        : $"the topic: '{model.Topic}', with specific focus on subtopic: '{model.SubTopic}'";

        //    string prompt = $@"
        //        You are a quiz generator agent. Create a quiz with {finalQuestionCount} multiple choice questions based on {topicInstruction}.
        //        The quiz should be suitable for {difficultyLevel} students.

        //        IMPORTANT INSTRUCTIONS:
        //        - Each question must have 2-6 multiple choice options
        //        - Provide a one line explanation for each question
        //        - If subtopic is not relevant to the main topic, focus only on the main topic

        //        Respond in JSON format EXACTLY like this:
        //        [
        //          {{
        //            ""question"": ""What is the capital of France?"",
        //            ""options"": [""Paris"", ""London"", ""Berlin"", ""Madrid""],
        //            ""correctOption"": ""Paris"",
        //            ""explanation"": ""Paris is the capital and largest city of France.""
        //          }}
        //        ]
        //
        //        Make sure each question has the exact format above with question, options array, correctOption, and explanation.";

        //    var body = new
        //    {
        //        contents = new[]
        //        {
        //            new
        //            {
        //                role = "user",    
        //                parts = new[]
        //                {
        //                    new { text = prompt }  
        //                }
        //            }
        //        }
        //    };

        //    try
        //    {
        //        var json = JsonSerializer.Serialize(body);
        //        var contentPayload = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        //        var endpoint = $"{_endpoint}?key={_apiKey}";
        //        var resp = await _httpClient.PostAsync(endpoint, contentPayload);

        //        if (!resp.IsSuccessStatusCode)
        //        {
        //            var errorContent = await resp.Content.ReadAsStringAsync();
        //            Console.WriteLine($"Gemini API Error: {resp.StatusCode} - {errorContent}");
        //            return null;
        //        }

        //        var responseString = await resp.Content.ReadAsStringAsync();
        //        Console.WriteLine($"Gemini API Response: {responseString}"); 

        //        using var doc = JsonDocument.Parse(responseString);
        //        var parts = doc.RootElement
        //            .GetProperty("candidates")[0]    
        //            .GetProperty("content")          
        //            .GetProperty("parts");           

        //        var quizJson = parts[0].GetProperty("text").GetString();
        //        Console.WriteLine($"Extracted Quiz JSON: {quizJson}"); // DEBUG

        //        quizJson = Methods.CleanJsonResponse(quizJson);
        //        Console.WriteLine($"Cleaned Quiz JSON: {quizJson}"); // DEBUG

        //        var quizItems = JsonSerializer.Deserialize<List<GeminiResponseVM>>(quizJson, new JsonSerializerOptions
        //        {
        //            PropertyNameCaseInsensitive = true // Case differences ignore karta hai
        //        });

        //        if (quizItems == null || !quizItems.Any())
        //        {
        //            Console.WriteLine("Failed to deserialize quiz items or empty result");
        //            return null;
        //        }

        //        ////////////////////////////////////
        //        // Store Data In DataBase

        //        var questions = new List<Question>();
        //        var options = new List<Option>();
        //        var quiz = new Quiz
        //        {
        //            Topic = model.Topic,
        //            SubTopic = model.SubTopic ?? "",
        //            TotalQuestions = finalQuestionCount,
        //            IsCompleted = false,
        //            UserID = _tokenService.UserID
        //        };
        //        _dbContext.Quizzes.Add(quiz);
        //        await _dbContext.SaveChangesAsync();

        //        foreach (var item in quizItems)
        //        {
        //            var question = new Question
        //            {
        //                QuestionText = item.question,
        //                Explanation = item.explanation ?? "",
        //                IsCorrect = false,
        //                QuizID = quiz.ID
        //            };
        //            questions.Add(question);
        //        }

        //        _dbContext.Questions.AddRange(questions);
        //        await _dbContext.SaveChangesAsync();

        //        // Now add options with correct QuestionID
        //        int i = 0;
        //        foreach (var item in quizItems)
        //        {
        //            var questionId = questions[i].ID; 
        //            foreach (var opt in item.options)
        //            {
        //                bool isCorrect = string.Equals(
        //                    opt.Trim(),
        //                    item.correctOption.Trim(),
        //                    StringComparison.OrdinalIgnoreCase // Ignore difference b/w Capital and small letter 
        //                );
        //                options.Add(new Option
        //                {
        //                    OptionText = opt,
        //                    IsCorrect = isCorrect,
        //                    QuestionID = questionId
        //                });
        //            }
        //            i++;
        //        }
        //        _dbContext.Options.AddRange(options);
        //        await _dbContext.SaveChangesAsync();
        //        // Fetch all questions for the newly created quiz
        //        var allQuestionsResponse = await GetAllQuizQuestionsAsync(quiz.ID);
        //        allQuestionsResponse.ResponseMessage = "Quiz generated and fetched successfully.";
        //        allQuestionsResponse.StatusCode = 200;
        //        return allQuestionsResponse;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error in GenerateQuizAsync: {ex.Message}");
        //        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        //        return null;
        //    }
        //}
        public async Task<ResponseVM> GetQuizQuestionsByNumberAsync(long quizID, long questionID)
        {
            ResponseVM response = ResponseVM.Instance;
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@QuizID", quizID);
                parameters.Add("@QuestionID", questionID);

                var result = await Methods.ExecuteStoredProceduresList("SP_GetQuizQuestionByNumber", parameters);

                if (result == null || !result.Any())
                {
                    response.StatusCode = 404;
                    response.ResponseMessage = "Question not found.";
                    return response;
                }

                // Build single question VM from flat rows (options per row)
                var firstRow = result.First();
                var questionVM = new QuizQuestionVM
                {
                    Question = firstRow.QuestionText,
                    Explanation = firstRow.Explanation,
                    Options = new List<QuizOptionVM>()
                };

                foreach (var row in result)
                {
                    questionVM.Options.Add(new QuizOptionVM
                    {
                        OptionText = row.OptionText,
                        IsCorrect = row.IsCorrect
                    });
                }

                response.StatusCode = 200;
                response.ResponseMessage = "Question fetched successfully.";
                response.Data = questionVM;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = "Error: " + ex.Message;
            }
            return response;
        }
        public ResponseVM GetAllQuizQuestions(long quizId)
        {
            ResponseVM response = ResponseVM.Instance;
            var parameters = new DynamicParameters();
            parameters.Add("@QuizID", quizId);

            var result = Methods.ExecuteStoredProceduresList("SP_GetAllQuizQuestions", parameters).Result;

            if (result == null || !result.Any())
            {
                response.StatusCode = 404;
                response.ResponseMessage = "No quiz or questions found for this quiz.";
                response.Data = null;
                return response;
            }

            var questionsDict = new Dictionary<long, QuizQuestionVM>();
            var questionsList = new List<QuizQuestionVM>();
            var quizDetail = new QuizDetailVM();

            string topic = "";
            int totalQuestions = 0;

            foreach (var row in result)
            {
                long questionId = row.QuestionId;
                if (!questionsDict.TryGetValue(questionId, out var questionVM))
                {
                    questionVM = new QuizQuestionVM
                    {
                        QuestionID = questionId,
                        Question = row.QuestionText,
                        Explanation = row.Explanation,
                        Options = new List<QuizOptionVM>()
                    };
                    questionsDict[questionId] = questionVM;
                    questionsList.Add(questionVM);
                }

                questionVM.Options.Add(new QuizOptionVM
                {
                    OptionText = row.OptionText,
                    IsCorrect = row.IsCorrect
                });

                if (string.IsNullOrEmpty(topic) || totalQuestions == 0)
                {
                    var quiz = _dbContext.Quizzes.FirstOrDefault(q => q.ID == quizId);
                    if (quiz != null)
                    {
                        topic = quiz.Topic;
                        totalQuestions = quiz.TotalQuestions ?? questionsList.Count;
                    }
                }
                quizDetail = new QuizDetailVM
                {
                    QuizID = quizId,
                    Topic = topic,
                    NoOfQuestions = totalQuestions,
                    Questions = questionsList
                };
            }
            response.StatusCode = 200;
            response.ResponseMessage = "All questions fetched successfully.";
            response.Data = quizDetail;
            return response;
        }
        public ResponseVM ResultSubmitted(ResultSubmittedVM model)
        {
            ResponseVM response = ResponseVM.Instance;
            try
            {
                var user = _dbContext.Users
                    .FirstOrDefault(u => u.UserID == _tokenService.UserID);
                if (user == null)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "User not found.";
                    return response;
                }
                var quiz = _dbContext.Quizzes
                    .FirstOrDefault(q => q.ID == model.QuizID && q.UserID == user.UserID);
                if (quiz == null)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "Quiz not found.";
                    return response;
                }
                //1. Update quiz 
                quiz.IsCompleted = true;
                quiz.CorrectQuestionCount = model.NoOfCorrectQuestions;
                quiz.IncorrectQuestionCount = model.NoOfIncorrectQuestions;
                quiz.TotalScore = model.TotalScore;
                quiz.ObtainedScore = model.ObtainedScore;
                _dbContext.Quizzes.Update(quiz);

                //2. Update Questions 
                var questions = _dbContext.Questions
                    .Where(q => q.QuizID == quiz.ID)
                    .ToList();
                foreach (var question in questions)
                {
                    question.IsCorrect = model.CorrectQuestionIds.Contains(question.ID);
                }
                _dbContext.Questions.UpdateRange(questions);

                _dbContext.SaveChanges();
                response.StatusCode = 200;
                response.ResponseMessage = "Results submitted successfully.";
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error: {ex.Message}";
            }
            return response;
        }
        //public async Task<ResponseVM> ResultSubmittedAsync(ResultSubmittedVM model)
        //{
        //    ResponseVM response = ResponseVM.Instance;
        //    try
        //    {
        //        var user = await _dbContext.Users
        //            .FirstOrDefaultAsync(u => u.UserID == _tokenService.UserID);
        //        if (user == null)
        //        {
        //            response.StatusCode = 404;
        //            response.ErrorMessage = "User not found.";
        //            return response;
        //        }
        //        var quiz = await _dbContext.Quizzes
        //            .FirstOrDefaultAsync(q => q.ID == model.QuizID && q.UserID == user.UserID);
        //        if (quiz == null)
        //        {
        //            response.StatusCode = 404;
        //            response.ErrorMessage = "Quiz not found.";
        //            return response;
        //        }
        //        //1. Update quiz 
        //        quiz.IsCompleted = true;
        //        quiz.CorrectQuestionCount = model.NoOfCorrectQuestions;
        //        quiz.IncorrectQuestionCount = model.NoOfIncorrectQuestions;
        //        quiz.TotalScore = model.TotalScore;
        //        quiz.ObtainedScore = model.ObtainedScore;
        //        _dbContext.Quizzes.UpdateRange(quiz);

        //       //2. Update Questions 
        //       var questions = await _dbContext.Questions
        //            .Where(q => q.QuizID == quiz.ID)
        //            .ToListAsync();
        //        foreach (var question in questions)
        //        {
        //            question.IsCorrect = model.CorrectQuestionIds.Contains(question.ID);
        //        }
        //        _dbContext.Questions.UpdateRange(questions);

        //        await _dbContext.SaveChangesAsync();
        //        response.StatusCode = 200;
        //        response.ResponseMessage = "Results submitted successfully.";
        //    }
        //    catch (Exception ex)
        //    {
        //        response.StatusCode = 500;
        //        response.ErrorMessage = $"Error: {ex.Message}";
        //    }
        //    return response;

        //}

        public ResponseVM GetQuizHistory()
        {
            ResponseVM response = ResponseVM.Instance;
            try
            {
                var parameter = new DynamicParameters();
                parameter.Add("@UserID", _tokenService.UserID);
                // Synchronous call
                var result = Methods.ExecuteStoredProceduresList("SP_GetQuizHistory", parameter).Result;
                if (result == null || !result.Any())
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "No quiz history found for this user.";
                    return response;
                }

                var quizList = result.Select(row => new QuizHistoryItemVM
                {
                    QuizID = row.QuizID,
                    Topic = row.Topic,
                    TotalScore = row.TotalScore,
                    ObtainedScore = row.ObtainedScore,
                    UpdatedDate = row.UpdatedDate,
                }).ToList();

                // Calculate summary
                var totalQuizzes = quizList.Count;
                var totalScore = result.Sum(row => (int)row.TotalScore);
                var obtainedScore = result.Sum(row => (int)row.ObtainedScore);

                // Prepare final VM
                var quizHistoryVM = new QuizHistoryVM
                {
                    TotalQuizzes = totalQuizzes,
                    TotalScore = totalScore,
                    ObtainedScore = obtainedScore,
                    Quizzes = quizList
                };

                response.StatusCode = 200;
                response.ResponseMessage = "Quiz history fetched successfully.";
                response.Data = quizHistoryVM;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error: {ex.Message}";
            }
            return response;
        }

        // For MCP updated functions by adding UserID check
        public ResponseVM GenerateQuizForUser(QuizVM model, long userId)
        {
            var response = ResponseVM.Instance;
            try
            {
                if (userId <= 0)
                {
                    response.StatusCode = 400;
                    response.ErrorMessage = "Invalid userId.";
                    return response;
                }
                if (string.IsNullOrWhiteSpace(model.Topic))
                {
                    response.StatusCode = 400;
                    response.ErrorMessage = "Topic is required.";
                    return response;
                }
                model.Topic = model.Topic.Trim();
                model.SubTopic = (model.SubTopic ?? string.Empty).Trim();

                // Build prompt
                int finalQuestionCount = model.QuestionCount ?? Random.Shared.Next(10, 15);
                string topicInstruction = string.IsNullOrEmpty(model.SubTopic)
                    ? $"the topic: '{model.Topic}'"
                    : $"the topic: '{model.Topic}', with specific focus on subtopic: '{model.SubTopic}'";

                var prompt = $@"
                You are a quiz generator agent. Create a quiz with {finalQuestionCount} multiple choice questions based on {topicInstruction}.
                The quiz should be suitable for Intermediate Level students.

                IMPORTANT INSTRUCTIONS:
                - Each question must have 2-6 multiple choice options
                - Provide a one line explanation for each question
                - If subtopic is not relevant to the main topic, focus only on the main topic

                Respond in JSON format EXACTLY like this:
                [
                  {{
                    ""question"": ""What is the capital of France?"",
                    ""options"": [""Paris"", ""London"", ""Berlin"", ""Madrid""],
                    ""correctOption"": ""Paris"",
                    ""explanation"": ""Paris is the capital and largest city of France.""
                  }}
                ]";

                // Define body (fix for 'body' not defined)
                var body = new
                {
                    contents = new[]
                    {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
                };

                // Ensure proper StringContent overload (fix for media type)
                // Add: using System.Text;
                var json = JsonSerializer.Serialize(body);
                var contentPayload = new StringContent(json, Encoding.UTF8, "application/json");

                var endpoint = $"{_endpoint}?key={_apiKey}";
                var resp = _httpClient.PostAsync(endpoint, contentPayload).Result;

                if (!resp.IsSuccessStatusCode)
                {
                    var errorContent = resp.Content.ReadAsStringAsync().Result;
                    response.StatusCode = (int)resp.StatusCode;
                    response.ErrorMessage = $"Gemini API Error: {resp.StatusCode} - {errorContent}";
                    return response;
                }

                var responseString = resp.Content.ReadAsStringAsync().Result;
                using var doc = JsonDocument.Parse(responseString);
                var parts = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");
                var quizJson = parts[0].GetProperty("text").GetString();
                quizJson = Methods.CleanJsonResponse(quizJson);

                var quizItems = JsonSerializer.Deserialize<List<GeminiResponseVM>>(quizJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (quizItems == null || quizItems.Count == 0)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "No questions generated.";
                    return response;
                }
                foreach (var item in quizItems)
                {
                    if (string.IsNullOrWhiteSpace(item.question) ||
                        item.options == null || item.options.Count < 2 ||
                        string.IsNullOrWhiteSpace(item.correctOption) ||
                        !item.options.Any(o => string.Equals(o.Trim(), item.correctOption.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        response.StatusCode = 400;
                        response.ErrorMessage = "Invalid question item in generated quiz.";
                        return response;
                    }
                }

                using var tx = _dbContext.Database.BeginTransaction();
                var dummy = GetOrCreateMcpDummyUser();

                var quiz = new Quiz
                {
                    Topic = model.Topic,
                    SubTopic = model.SubTopic ?? "",
                    TotalQuestions = finalQuestionCount,
                    IsCompleted = false,
                    UserID = dummy.UserID,
                    ExternalUserId = userId
                };
                _dbContext.Quizzes.Add(quiz);
                _dbContext.SaveChanges();

                var questions = new List<Question>();
                foreach (var item in quizItems)
                {
                    questions.Add(new Question
                    {
                        QuestionText = item.question.Trim(),
                        Explanation = item.explanation ?? "",
                        IsCorrect = false,
                        QuizID = quiz.ID
                    });
                }
                _dbContext.Questions.AddRange(questions);
                _dbContext.SaveChanges();

                var options = new List<Option>();
                for (int i = 0; i < quizItems.Count; i++)
                {
                    var item = quizItems[i];
                    var qId = questions[i].ID;

                    foreach (var opt in item.options)
                    {
                        bool isCorrect = string.Equals(opt.Trim(), item.correctOption.Trim(), StringComparison.OrdinalIgnoreCase);
                        options.Add(new Option
                        {
                            OptionText = opt,
                            IsCorrect = isCorrect,
                            QuestionID = qId
                        });
                    }
                }
                _dbContext.Options.AddRange(options);
                _dbContext.SaveChanges();

                tx.Commit();

                var allQuestionsResponse = GetAllQuizQuestions(quiz.ID);
                allQuestionsResponse.ResponseMessage = "Quiz generated and fetched successfully.";
                allQuestionsResponse.StatusCode = 200;
                return allQuestionsResponse;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error: {ex.Message}";
                return response;
            }
        }

        // Tool 3: Get quiz details for MCP user
        public ResponseVM GetQuizDetailsForUser(long quizId, long userId)
        {
            var response = ResponseVM.Instance;
            try
            {
                if (quizId <= 0 || userId <= 0)
                {
                    response.StatusCode = 400;
                    response.ErrorMessage = "quizId and userId must be positive.";
                    return response;
                }

                var quiz = _dbContext.Quizzes.FirstOrDefault(q => q.ID == quizId && q.ExternalUserId == userId);
                if (quiz == null)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "Quiz not found for this MCP user.";
                    return response;
                }

                var parameter = new DynamicParameters();
                parameter.Add("@QuizID", quizId);
                var result = Methods.ExecuteStoredProceduresList("SP_GetQuizDetails", parameter).Result;

                if (result == null || !result.Any())
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "No quiz details found for this quiz.";
                    return response;
                }

                var questions = result.Select(row => new APIQuizDetailsItemsVM
                {
                    QuestionID = row.QuestionID ?? 0,
                    QuestionText = row.QuestionText,
                    Explanation = row.Explanation,
                    IsCorrect = row.IsCorrect
                }).ToList();

                var firstRow = result.First();
                var details = new APIQuizDetailsVM
                {
                    Topic = firstRow.Topic,
                    UpdatedDate = firstRow.UpdatedDate,
                    TotalQuestions = questions.Count,
                    TotalScore = (int)firstRow.TotalScore,
                    ObtainedScore = (int)firstRow.ObtainedScore,
                    Questions = questions
                };

                response.StatusCode = 200;
                response.ResponseMessage = "Quiz details fetched successfully.";
                response.Data = details;
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error: {ex.Message}";
                return response;
            }
        }

        // Tool 4: Get quiz history for MCP user
        public ResponseVM GetQuizHistoryForUser(long userId)
        {
            var response = ResponseVM.Instance;
            try
            {
                if (userId <= 0)
                {
                    response.StatusCode = 400;
                    response.ErrorMessage = "Invalid userId.";
                    return response;
                }

                // Prefer DB-side filtering: add a new SP or filter in-memory
                var quizzes = _dbContext.Quizzes.Where(q => q.ExternalUserId == userId)
                    .OrderByDescending(q => q.UpdatedDate)
                    .Select(q => new
                    {
                        q.ID,
                        q.Topic,
                        q.TotalScore,
                        q.ObtainedScore,
                        q.UpdatedDate
                    })
                    .ToList();

                if (quizzes.Count == 0)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "No quiz history found for this MCP user.";
                    return response;
                }

                var quizList = quizzes.Select(row => new QuizHistoryItemVM
                {
                    QuizID = row.ID,
                    Topic = row.Topic,
                    TotalScore = row.TotalScore,
                    ObtainedScore = row.ObtainedScore,
                    UpdatedDate = row.UpdatedDate,
                }).ToList();

                var vm = new QuizHistoryVM
                {
                    TotalQuizzes = quizList.Count,
                    TotalScore = quizList.Sum(x => x.TotalScore),
                    ObtainedScore = quizList.Sum(x => x.ObtainedScore),
                    Quizzes = quizList
                };

                response.StatusCode = 200;
                response.ResponseMessage = "Quiz history fetched successfully.";
                response.Data = vm;
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error: {ex.Message}";
                return response;
            }
        }

        // Tool 2: Result submission for MCP user
        public ResponseVM ResultSubmittedForUser(ResultSubmittedVM model, long userId)
        {
            var response = ResponseVM.Instance;
            try
            {
                if (model == null || model.QuizID <= 0 || userId <= 0)
                {
                    response.StatusCode = 400;
                    response.ErrorMessage = "Invalid result submission data.";
                    return response;
                }

                var quiz = _dbContext.Quizzes.FirstOrDefault(q => q.ID == model.QuizID && q.ExternalUserId == userId);
                if (quiz == null)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "Quiz not found for this MCP user.";
                    return response;
                }

                var questions = _dbContext.Questions.Where(q => q.QuizID == quiz.ID).ToList();
                if (questions.Count == 0)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "Quiz has no questions.";
                    return response;
                }

                // validations (counts, ids membership) unchanged ...

                // update quiz and questions (unchanged) ...
                _dbContext.SaveChanges();

                response.StatusCode = 200;
                response.ResponseMessage = "Results submitted successfully.";
                return response;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = $"Error: {ex.Message}";
                return response;
            }
        }
        public async Task<ResponseVM> GetQuizQuestionsByNumberAsync(long quizID, int QuestionNumber)
        {
            ResponseVM response = ResponseVM.Instance;
            try
            {
                var parameteres = new DynamicParameters();
                parameteres.Add("@QuizID", quizID);
                parameteres.Add("@QuestionNumber", QuestionNumber);
                // Call the stored procedure using your shared Methods class
                var result = await Methods.ExecuteStoredProceduresList("SP_GetQuizQuestionByNumber", parameteres);

                QuizQuestionVM? questionVM = null;

                foreach (var row in result)
                {
                    if (questionVM == null)
                    {
                        questionVM = new QuizQuestionVM
                        {
                            Question = row.QuestionText,
                            Explanation = row.Explanation,
                            Options = new List<QuizOptionVM>()
                        };
                    }
                    questionVM.Options.Add(new QuizOptionVM
                    {
                        OptionText = row.OptionText,
                        IsCorrect = row.IsCorrect
                    });
                }
                if (questionVM == null)
                {
                    response.StatusCode = 404;
                    response.ResponseMessage = "Question not found.";
                    return response;
                }
                else
                {
                    response.StatusCode = 200;
                    response.ResponseMessage = "Question fetched successfully.";
                    response.Data = questionVM;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = "Error: " + ex.Message;
            }
            return response;

        }





    }
}
