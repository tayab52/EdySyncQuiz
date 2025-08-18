using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferModels.QuizViewModels
{
    public class ResultSubmittedVM
    {
        public long QuizID { get; set; }
        public int NoOfCorrectQuestions { get; set; }
        public int NoOfIncorrectQuestions { get; set; }
        public int TotalScore { get; set; }
        public int ObtainedScore { get; set; }
        public List<long> CorrectQuestionIds { get; set; } = new();
    }
}
