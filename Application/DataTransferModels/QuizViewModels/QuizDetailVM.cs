using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferModels.QuizViewModels
{
    public class QuizDetailVM
    {
        public long QuizID { get; set; }
        public string Topic { get; set; } = string.Empty;
        public int NoOfQuestions { get; set; }
        public List<QuizQuestionVM> Questions { get; set; } = new();
    }
}
