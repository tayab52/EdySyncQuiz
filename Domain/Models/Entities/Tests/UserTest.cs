using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Domain.Models.Entities.Answers;
using Domain.Models.Entities.Questions;
using Domain.Models.Entities.Users;

namespace Domain.Models.Entities.Tests
{
    public class UserTest
    {
        [Key]
        public Guid UserTestID { get; set; }

        [Column("TestDate", TypeName = "datetime2")]
        public DateTime TestDate { get; set; }

        [Column("CorrectAnswers")]
        public int? CorrectAnswers { get; set; }

        [Column("Status")]
        public string? Status { get; set; } = "Pending"; // ("Pending", "Completed", "Abandoned")

        [JsonIgnore]
        [ForeignKey("User")]
        public Guid UserID { get; set; }

        public User User { get; set; } = null!;

        public List<Question> Questions { get; set; } = [];

        public List<UserAnswer> UserAnswers { get; set; } = [];
    }
}
