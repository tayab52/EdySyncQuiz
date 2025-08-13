using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DataTransferModels.QuizViewModels
{
    public class QuizOptionVM
    {
       
            public string OptionText { get; set; } = string.Empty;

            public bool IsCorrect { get; set; }
        
    }
}
