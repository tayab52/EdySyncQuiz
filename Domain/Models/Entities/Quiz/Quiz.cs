using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Domain.Models.Entities.Answers;
using Domain.Models.Entities.Questions;
using Domain.Models.Entities.Users;

namespace Domain.Models.Entities.Quiz
{
    public class Quiz
    {
        [Key]
        public Guid QuizID { get; set; }

        public string Topic { get; set; } = string.Empty;

        [Column(TypeName = "datetime2")]
        public DateTime TestDate { get; set; }

        public string? Status { get; set; } = "Pending"; // ("Pending", "Completed", "Abandoned")

        public int? Score { get; set; } = 0;

        [ForeignKey("User")]
        public Guid UserID { get; set; }
    }
}
