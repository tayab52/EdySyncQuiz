using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Entities.Users
{
    public class TestCategory
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("CategoryId")]
        [Key]
        public int CategoryId { get; set; }

        [Column("Name")]
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("Description")]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public List<UserTest> UserTests { get; set; } = [];
    }
}
