using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DataTransferModels.Quiz
{
    public class QuizQuestionDTO
    {
        [JsonPropertyName("question")]
        public string Question { get; set; } = "";
        [JsonPropertyName("options")]
        public Dictionary<string, string> Options { get; set; } = new();
        [JsonPropertyName("answer")]
        public string Answer { get; set; } = "";
    }
}
