using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferModels.QuizViewModels
{
    public class QuizVM
    {
        public string Topic { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
        public string? SubTopic { get; set; } = null;
        public int? QuestionCount { get; set; } = null;

    }
}
