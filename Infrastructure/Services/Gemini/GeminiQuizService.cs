using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Utilities;
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

        public async Task<string> GenerateQuizFromInterestsAsync(string interests, int level)
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
                      ...
                    ]";

            var requestBody = new GeminiRequest
            {
                contents = new List<GeminiContent>
                {
                    new()
                    {
                        parts = new List<GeminiPart>
                        {
                            new() { text = prompt }
                        }
                    }
                },
                generationConfig = new GeminiGenerationConfig
                {
                    temperature = 0.2,
                    topK = 20,
                }
            };

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{URI}?key={ApiKey}"),
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            var sw = Stopwatch.StartNew();
            var response = await HttpClient.SendAsync(request);
            sw.Stop();
            Console.WriteLine($"Gemini API took {sw.ElapsedMilliseconds} ms");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var root = JsonDocument.Parse(json);
            var text = root.RootElement
                           .GetProperty("candidates")[0]
                           .GetProperty("content")
                           .GetProperty("parts")[0]
                           .GetProperty("text")
                           .GetString();

            return text ?? string.Empty;
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
