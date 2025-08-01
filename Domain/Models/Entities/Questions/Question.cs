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

        [ForeignKey("Quiz")]
        public Guid QuizID { get; set; }
    }
}
