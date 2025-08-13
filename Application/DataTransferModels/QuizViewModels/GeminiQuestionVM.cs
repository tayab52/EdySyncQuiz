using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferModels.QuizViewModels
{

    public class GeminiQuestionVM
    {
        public string Question { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public string Answer { get; set; } = string.Empty;
    }
}
