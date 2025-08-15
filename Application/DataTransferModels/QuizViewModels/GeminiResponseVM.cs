using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferModels.QuizViewModels
{
    public class GeminiResponseVM
    {
        public string question { get; set; } = string.Empty;
        public List<string> options { get; set; } = new();
        public string correctOption { get; set; } = string.Empty;
        public string explanation { get; set; } = string.Empty;

    }
}
