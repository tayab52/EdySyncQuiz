using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Models.Entities.Users
{
    public class UserTest
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("UserTestId")]
        [Key]
        public int UserTestId { get; set; }

        [ForeignKey("User")]
        [Column("UserId")]
        public int UserId { get; set; }

        [ForeignKey("TestCategory")]
        [Column("CategoryId")]
        public int CategoryId { get; set; }

        [Column("TestDate", TypeName = "datetime2")]
        public DateTime TestDate { get; set; }

        [Column("Score", TypeName = "decimal(5, 2)")]
        public decimal? Score { get; set; }

        [Column("TotalQuestions")]
        public int? TotalQuestions { get; set; }

        [Column("CorrectAnswers")]
        public int? CorrectAnswers { get; set; }

        [Column("Status")]
        [Required]
        public string Status { get; set; } = "Pending"; // ("Pending", "Completed", "Abandoned")

        [Column("GeminiPromptUsed", TypeName = "nvarchar(max)")]
        public string? GeminiPromptUsed { get; set; }

        [JsonIgnore]
        public User User { get; set; } = null!;

        [JsonIgnore]
        public TestCategory TestCategory { get; set; } = null!;

        public List<Question> Questions { get; set; } = [];

        public List<UserAnswer> UserAnswers { get; set; } = [];
    }
}
