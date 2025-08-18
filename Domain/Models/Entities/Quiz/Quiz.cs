using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Domain.Models.BaseEntities;
using Domain.Models.Entities.Answers;
using Domain.Models.Entities.Questions;
using Domain.Models.Entities.Users;

namespace Domain.Models.Entities.Quiz
{
    public class Quiz : BaseEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long ID { get; set; }
        [Column(TypeName = "NVARCHAR(250)")]       
        public string Topic { get; set; } = string.Empty;
        [Column(TypeName = "NVARCHAR(250)")]
        public string SubTopic { get; set; } = string.Empty;
        public int? TotalQuestions { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
        public long UserID { get; set; }

        public int CorrectQuestionCount { get; set; } = 0;
        public int IncorrectQuestionCount { get; set; } = 0;
        public int TotalScore { get; set; } = 0;
        public int ObtainedScore { get; set; } = 0;
    }
}
