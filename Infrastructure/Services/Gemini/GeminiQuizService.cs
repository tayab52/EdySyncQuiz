using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services.Gemini
{
    public class GeminiQuizService(IConfiguration config)
    {
        private readonly HttpClient HttpClient = new HttpClient();
        private readonly string ApiKey = config["Gemini:ApiKey"]!;
        private readonly string URI = config["Gemini:Endpoint"]!;

        public async Task<string?> GenerateQuizFromInterestsAsync(string interests, int level)
        {
            string prompt = $@"
                    Create a quiz with 10 multiple choice questions based on the following topics: {interests}. Make sure to include questions from all the listed interests
                    and if there are more than 10 interests, choose randomnly. Shuffle the questions as well.
                    The quiz should be suitable for a level {level} audience.
                    
                    Difficulty levels:
                    - Level 0: Entry Level, beginnner knowledge 
                    - Level 1: Basic Level, simple concepts
                    - Level 2: Middle Level, intermediate knowledge
                    - Level 3: Top Level, advanced concepts

                    Each question should have:
                    - A question statement
                    - Four options labeled A, B, C, D
                    - Correct answer letter
                    
                    Respond in JSON format like:
                    [
                      {{
                        ""question"": ""..."",
                        ""options"": {{""A"": ""..."", ""B"": ""..."", ""C"": ""..."", ""D"": ""...""}},
                        ""answer"": ""A""
                      }},
                    ]";

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

            var json = System.Text.Json.JsonSerializer.Serialize(body);
            var contentPayload = new StringContent(json, Encoding.UTF8, "application/json");

            var timeElapsed = Stopwatch.StartNew();

            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={ApiKey}";
            var resp = await HttpClient.PostAsync(endpoint, contentPayload);
            timeElapsed.Stop();
            Console.WriteLine("Time Elapsed: " + timeElapsed.ElapsedMilliseconds);

            if (!resp.IsSuccessStatusCode)
                return null;

            var responseString = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            var parts = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts");
            var response = parts[0].GetProperty("text").GetString();
            return response;
        }

        public class GeminiRequest
        {
            public List<GeminiContent> contents { get; set; } = new();
            public GeminiGenerationConfig generationConfig { get; set; } = new();
        }

        public class GeminiGenerationConfig
        {
            public double temperature { get; set; }
            public int topK { get; set; }
        }

        public class GeminiContent
        {
            public List<GeminiPart> parts { get; set; } = new();
        }

        public class GeminiPart
        {
            public string text { get; set; } = "";
        }
    }
}
