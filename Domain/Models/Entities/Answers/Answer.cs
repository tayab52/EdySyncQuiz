using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Domain.Models.BaseEntities;

namespace Domain.Models.Entities.Answers
{
    public class Answer : BaseEntity
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }
        public long QuestionID { get; set; }
        public long OptionID { get; set; }
        public long USERID { get; set; }


    }
}
