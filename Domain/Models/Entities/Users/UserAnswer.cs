using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Models.Entities.Users
{
    public class UserAnswer
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("UserAnswerId")]
        [Key]
        public int UserAnswerId { get; set; }

        [ForeignKey("User")]
        [Column("UserId")]
        public int UserId { get; set; }

        [ForeignKey("Question")]
        [Column("QuestionId")]
        public int QuestionId { get; set; }

        [ForeignKey("UserTest")]
        [Column("UserTestId")]
        public int UserTestId { get; set; }

        [Column(TypeName = "nvarchar(1)")]
        [Required]
        public string? SelectedAnswerOption { get; set; } = string.Empty; // ('A', 'B', 'C', 'D', or null if skipped)

        [Column(TypeName = "bit")]
        public bool? IsCorrect { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime AnswerDate { get; set; }

        [JsonIgnore]
        public Question Question { get; set; } = null!;

        [JsonIgnore]
        public User User { get; set; } = null!;

        [JsonIgnore]
        public UserTest UserTest { get; set; } = null!;
    }
}
