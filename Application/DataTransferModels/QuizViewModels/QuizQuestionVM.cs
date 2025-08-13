using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DataTransferModels.QuizViewModels
{
    public class QuizQuestionVM
    {
        public string Question { get; set; } = string.Empty;

        public List<QuizOptionVM> Options { get; set; } = new();

        public string Explanation { get; set; } = string.Empty;
    }
}
