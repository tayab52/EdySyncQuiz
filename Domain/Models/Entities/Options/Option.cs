using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.Entities.Options
{
    public class Option
    {
        public Guid OptionID { get; set; }

        public string OptionText { get; set; } = string.Empty; 

        public bool IsCorrect { get; set; } = false; // Indicates if the option is correct

        [ForeignKey("Question")]
        public Guid QuestionID { get; set; } 
    }
}
