using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Models.Entities.Users
{
    public class Question
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("QuestionId")]
        [Key]
        public int QuestionId { get; set; }

        [ForeignKey("UserTest")]
        [Column("UserTestId")]
        public int UserTestId { get; set; }

        [Column(TypeName = "nvarchar(500)")]
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

        [Column("Explanation")]
        public string? Explanation { get; set; }

        [JsonIgnore]
        public UserTest UserTest { get; set; } = null!;

        public List<UserAnswer> UserAnswers { get; set; } = [];
    }
}
