using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Models.BaseEntities;

namespace Domain.Models.Entities.Options
{
    public class Option : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }
        [Column(TypeName = "NVARCHAR(500)")]
        public string OptionText { get; set; } = string.Empty; 
        public bool IsCorrect { get; set; } = false;
        public long QuestionID { get; set; } 
    }
}
