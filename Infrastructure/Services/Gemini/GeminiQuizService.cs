using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

using System.Text.Json.Serialization;

namespace Infrastructure.Services.Gemini
{
    public class GeminiQuizService(IConfiguration config)
    {
        private readonly HttpClient HttpClient = new HttpClient();
        private readonly string ApiKey = config["Gemini:ApiKey"]!;
        private readonly string URI = config["Gemini:Endpoint"]!;

        public async Task<string?> GenerateQuizFromInterestsAsync(string topic, int level, int questionCount = 10)
        {
            string difficultyLevel = level switch
            {
                0 => "Entry Level ",
                1 => "Basic Level",
                2 => "Middle Level",
                3 => "Advanced Level",
                _ => "Intermediate Level",
            };
            string prompt = $@"
                    You are a quiz generator agent. Create a quiz with {questionCount} multiple choice questions based on the topic: {topic}. 
                    The quiz should be suitable for a {difficultyLevel}.
                   
                    Each question should have:
                    - A question statement
                    - 2-6 options with just text
                    - Correct answer text
                    
                    Respond in JSON format like:
                    [
                      {{
                        ""question"": ""..."",
                        ""options"": [""..."", ""..."", ...],
                        ""answer"": ""...""
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
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=AIzaSyBIQTIgOWoIL1cMGpeyhAvXRjZ7RH7tQA0";
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

            return response!.Trim().Trim('`')[4..].Trim();
        }
    }
}
