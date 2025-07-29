using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services.Gemini
{
    public class GeminiQuizService(IConfiguration config)
    {
        private readonly HttpClient HttpClient = new HttpClient();
        private readonly string ApiKey = config["Gemini:ApiKey"]!;
        private readonly string URI = config["Gemini:Endpoint"]!;

        public async Task<string> GenerateQuizFromInterestsAsync(string interests)
        {
            string prompt = $@"
                    Create a quiz with 10 multiple choice questions based on the following topics: {interests}.
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

            var requestBody = new
            {
                contents = new[]
                {
                new { parts = new[] { new { text = prompt } } }
            }
            };

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{URI}?key={ApiKey}"),
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            var response = await HttpClient.SendAsync(request);
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
    }
}
