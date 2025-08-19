using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferModels.QuizViewModels
{
    public class QuizHistoryItemVM
    {
        public long QuizID { get; set; }
        public string Topic { get; set; } = string.Empty;
        public int TotalScore { get; set; } = 0;
        public int ObtainedScore { get; set; } = 0;

        public DateTime UpdatedDate { get; set; }

    }
}
