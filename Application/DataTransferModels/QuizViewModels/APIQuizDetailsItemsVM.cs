using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferModels.QuizViewModels
{
    public class APIQuizDetailsItemsVM
    {
        public long QuestionID { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }
}
