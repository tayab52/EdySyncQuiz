using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Util;

namespace Application.DataTransferModels.QuizViewModels
{
    public class QuizQuestionVM
    {
        public long QuestionID { get; set; }
        public string Question { get; set; } = string.Empty;

        public List<QuizOptionVM> Options { get; set; } = new();

        public string Explanation { get; set; } = string.Empty;
    }
}
