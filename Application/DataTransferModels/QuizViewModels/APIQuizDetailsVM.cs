using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferModels.QuizViewModels
{
    public class APIQuizDetailsVM
    {
        public string Topic { get; set; } = string.Empty;
        public DateTime UpdatedDate { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalScore { get; set; }
        public int ObtainedScore { get; set; }
        public List<APIQuizDetailsItemsVM> Questions { get; set; } = new();
    }
}
