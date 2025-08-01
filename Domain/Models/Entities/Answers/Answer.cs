using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Models.Entities.Answers
{
    public class Answer
    {
        [Key]
        public Guid AnswerID { get; set; }

        [ForeignKey("Question")]
        public Guid QuestionID { get; set; }

        [ForeignKey("Option")]
        public Guid OptionID { get; set; }
    }
}
