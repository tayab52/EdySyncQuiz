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

        public async Task<ResponseVM> GenerateQuizAsync(QuizVM model)
        {
            ResponseVM response = ResponseVM.Instance;
            string difficultyLevel = model.Level switch
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

                quizJson = CleanJsonResponse(quizJson);
                Console.WriteLine($"Cleaned Quiz JSON: {quizJson}"); // DEBUG

                var quizItems = JsonSerializer.Deserialize<List<QuizItem>>(quizJson, new JsonSerializerOptions
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
                await _dbContext.SaveChangesAsync();


                //var quizList = new List<QuizQuestionVM>();

                //foreach (var item in quizItems)
                //{
                //    var quiz = new QuizQuestionVM
                //    {
                //        Question = item.question,
                //        Explanation = item.explanation ?? "",
                //        Options = new List<QuizOptionVM>()
                //    };

                //    foreach (var opt in item.options)
                //    {
                //        bool isCorrect = string.Equals(
                //            opt.Trim(),
                //            item.correctOption.Trim(),
                //            StringComparison.OrdinalIgnoreCase
                //        );

                //        quiz.Options.Add(new QuizOptionVM
                //        {
                //            OptionText = opt,
                //            IsCorrect = isCorrect
                //        });
                //    }

                //    quizList.Add(quiz);
                //}
                response.ResponseMessage = "Question Generated Successfully";
                response.StatusCode = 200;
                return (response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateQuizAsync: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return null;
            }
        }

        private string CleanJsonResponse(string? jsonResponse)
        {
            if (string.IsNullOrEmpty(jsonResponse))
                return "[]";

            var cleaned = jsonResponse.Trim().Trim('`');

            if (cleaned.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(4).Trim();
            }

            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(7).Trim();
            }

            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3).Trim();
            }

            return cleaned;
        }

        private class QuizItem
        {
            public string question { get; set; } = string.Empty;       
            public List<string> options { get; set; } = new();         
            public string correctOption { get; set; } = string.Empty;  
            public string explanation { get; set; } = string.Empty;    
        }
    }
}
