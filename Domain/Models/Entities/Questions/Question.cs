using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Domain.Models.BaseEntities;

namespace Domain.Models.Entities.Questions
{
    public class Question : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        [Column(TypeName = "nvarchar(501)")]
        [Required]
        public string QuestionText { get; set; } = string.Empty;
        [Column(TypeName = "NVARCHAR(250)")]
        public string Explanation { get; set; } = string.Empty; 
        public bool IsCorrect { get; set; } = false; 
        public long QuizID { get; set; }
    }
}
