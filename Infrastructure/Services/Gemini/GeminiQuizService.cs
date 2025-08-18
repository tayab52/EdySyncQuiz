using Domain.Models.Entities.Questions;
using Domain.Models.Entities.Options;
using Domain.Models.Entities.Quiz;
using Domain.Models.Entities.Answers;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Application.DataTransferModels.QuizViewModels;
using Application.DataTransferModels.ResponseModel;
using Application.Interfaces.Gemini;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Services.Token;
using CommonOperations.Methods;
using Dapper;
using Azure;

namespace Infrastructure.Services.Gemini
{
    public class GeminiQuizService : IQuizService
    {
        private readonly HttpClient _httpClient = new();
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly AppDBContext _dbContext;
        private readonly TokenService _tokenService;


        public GeminiQuizService(IConfiguration config, AppDBContext dbContext, TokenService tokenService)
        {
            _apiKey = config["Gemini:ApiKey"]!;  
            _endpoint = config["Gemini:Endpoint"]!;
            _dbContext = dbContext;
            _tokenService = tokenService;

        }

        public async Task<ResponseVM> ResultSubmittedAsync(ResultSubmittedVM model)
        {
            ResponseVM response = ResponseVM.Instance;
            try
            {
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.UserID == _tokenService.UserID);
                if (user == null)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "User not found.";
                    return response;
                }
                var quiz = await _dbContext.Quizzes
                    .FirstOrDefaultAsync(q => q.ID == model.QuizID && q.UserID == user.UserID);
                if (quiz == null)
                {
                    response.StatusCode = 404;
                    response.ErrorMessage = "Quiz not found.";
                    return response;
                }
                //1. Update quiz completion status
                quiz.IsCompleted = true;
                quiz.CorrectQuestionCount = model.NoOfCorrectQuestions;
                quiz.IncorrectQuestionCount = model.NoOfIncorrectQuestions;
                quiz.TotalScore = model.TotalScore;
                quiz.ObtainedScore = model.ObtainedScore;
                _dbContext.Quizzes.UpdateRange(quiz);

               //2. Update Questions 
               var questions = await _dbContext.Questions
                    .Where(q => q.QuizID == quiz.ID)
                    .ToListAsync();
                foreach (var question in questions)
                {
                    question.IsCorrect = model.CorrectQuestionIds.Contains(question.ID);
                }
                _dbContext.Questions.UpdateRange(questions);

                await _dbContext.SaveChangesAsync();
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

        public async Task<ResponseVM> GenerateQuizAsync(QuizVM model)
        {
            ResponseVM response = ResponseVM.Instance;
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.UserID == _tokenService.UserID);
            //if (user == null || user.Level == null)
            //{
            //    response.StatusCode = 400;
            //    response.ErrorMessage = "User or user level not found.";
            //    return response;
            //}
            int userLevel = user?.Level.Value ?? 1; // Default to 1 if null

            string difficultyLevel = userLevel switch
            {
                0 => "Entry Level",
                1 => "Basic Level",
                2 => "Middle Level",
                3 => "Advanced Level",
                _ => "Intermediate Level",
            };

            int finalQuestionCount = model.QuestionCount ?? Random.Shared.Next(2, 7); 

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
                var resp = await _httpClient.PostAsync(endpoint, contentPayload);

                if (!resp.IsSuccessStatusCode)
                {
                    var errorContent = await resp.Content.ReadAsStringAsync();
                    Console.WriteLine($"Gemini API Error: {resp.StatusCode} - {errorContent}");
                    return null;
                }

                var responseString = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"Gemini API Response: {responseString}"); 

                using var doc = JsonDocument.Parse(responseString);
                var parts = doc.RootElement
                    .GetProperty("candidates")[0]    
                    .GetProperty("content")          
                    .GetProperty("parts");           

                var quizJson = parts[0].GetProperty("text").GetString();
                Console.WriteLine($"Extracted Quiz JSON: {quizJson}"); // DEBUG

                quizJson = Methods.CleanJsonResponse(quizJson);
                Console.WriteLine($"Cleaned Quiz JSON: {quizJson}"); // DEBUG

                var quizItems = JsonSerializer.Deserialize<List<GeminiResponseVM>>(quizJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // Case differences ignore karta hai
                });

                if (quizItems == null || !quizItems.Any())
                {
                    Console.WriteLine("Failed to deserialize quiz items or empty result");
                    return null;
                }

                ////////////////////////////////////
                // Store Data In DataBase

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
                await _dbContext.SaveChangesAsync();

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
                await _dbContext.SaveChangesAsync();

                // Now add options with correct QuestionID
                int i = 0;
                foreach (var item in quizItems)
                {
                    var questionId = questions[i].ID; 
                    foreach (var opt in item.options)
                    {
                        bool isCorrect = string.Equals(
                            opt.Trim(),
                            item.correctOption.Trim(),
                            StringComparison.OrdinalIgnoreCase // Ignore difference b/w Capital and small letter 
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
                await _dbContext.SaveChangesAsync();
                // Fetch all questions for the newly created quiz
                var allQuestionsResponse = await GetAllQuizQuestionsAsync(quiz.ID);

                // Optionally, you can set a custom message or status code here
                allQuestionsResponse.ResponseMessage = "Quiz generated and fetched successfully.";
                allQuestionsResponse.StatusCode = 200;

                return allQuestionsResponse;


                //response.ResponseMessage = "Question Generated Successfully";
                //response.StatusCode = 200;
                //return (response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateQuizAsync: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<ResponseVM> GetAllQuizQuestionsAsync(long quizId)
        {
            ResponseVM response = ResponseVM.Instance;
            var parameters = new DynamicParameters();
            parameters.Add("@QuizID", quizId);

            // Call the stored procedure using your shared Methods class
            var result = await Methods.ExecuteStoredProceduresList("SP_GetAllQuizQuestions", parameters);

            // Map the flat result to your VMs
            var questionsDict = new Dictionary<long, QuizQuestionVM>();
            var questionsList = new List<QuizQuestionVM>();
            var quizDetail = new QuizDetailVM();

            string topic = "";
            int totalQuestions =0;


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

                // Get topic and total questions from the first row if i get from the stored Procedure 
                //if (string.IsNullOrEmpty(topic) && row.Topic != null)
                //{
                //    topic = row.Topic;
                //}

                //if (row.TotalQuestions != null)
                //{
                //    totalQuestion = row.TotalQuestions.Value;
                //}

                if (string.IsNullOrEmpty(topic) || totalQuestions == 0)
                {
                    var quiz = await _dbContext.Quizzes.FirstOrDefaultAsync(q => q.ID == quizId);
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
