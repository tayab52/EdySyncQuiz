using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferModels.QuizViewModels
{
    public class QuizHistoryVM
    {
        public int TotalQuizzes { get; set; }
        public int TotalScore { get; set; }
        public int ObtainedScore { get; set; }
        public List<QuizHistoryItemVM> Quizzes { get; set; } = new();
    }


}
