using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Models.Entities.Users
{
    public class UserInterest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InterestID { get; set; }

        [Required]
        [MaxLength(100)]
        public string InterestName { get; set; } = string.Empty;

        public int UserID { get; set; }

        [ForeignKey("UserID")]
        [JsonIgnore]
        public User User { get; set; } = null!;
    }
}
