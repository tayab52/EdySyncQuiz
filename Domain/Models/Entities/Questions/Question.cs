using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Models.Entities.Questions
{
    public class Question
    {
        [Key]
        public Guid QuestionID { get; set; }

        [Column(TypeName = "nvarchar(501)")]
        [Required]
        public string QuestionText { get; set; } = string.Empty;

        [Column("OptionA")]
        public string OptionA { get; set; } = string.Empty;

        [Column("OptionB")]
        public string OptionB { get; set; } = string.Empty;

        [Column("OptionC")]
        public string OptionC { get; set; } = string.Empty;

        [Column("OptionD")]
        public string OptionD { get; set; } = string.Empty;

        [Column("CorrectAnswerOption")]
        [Required]
        public string CorrectAnswerOption { get; set; } = string.Empty; // ('A', 'B', 'C', or 'D')
    }
}
