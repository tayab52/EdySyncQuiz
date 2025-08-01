using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Models.Entities.Answers
{
    public class UserAnswer
    {
        [Key]
        public Guid UserAnswerId { get; set; }

        [ForeignKey("User")]
        public Guid UserID { get; set; }

        [ForeignKey("Question")]
        public Guid QuestionID { get; set; }

        [ForeignKey("UserTest")]
        public Guid UserTestID { get; set; }

        [Column(TypeName = "nvarchar(1)")]
        public string? SelectedAnswerOption { get; set; } = string.Empty; // ('A', 'B', 'C', 'D', or null if skipped)

        [Column(TypeName = "bit")]
        public bool? IsCorrect { get; set; }
    }
}
