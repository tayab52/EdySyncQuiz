using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.BaseEntities
{
    public class BaseEntity
    {
        public bool IsDeleted { get; set; } = false;
        [Column(TypeName = "NVARCHAR")]
        [MaxLength(300)]
        public string AddedBy { get; set; } = "";
        public DateTime AddedDate { get; set; }
        [Column(TypeName = "NVARCHAR")]
        [MaxLength(300)]
        public string UpdatedBy { get; set; } = "";
        public DateTime UpdatedDate { get; set; }
        // public bool IsActive { get; set; } = true;
    }
}
