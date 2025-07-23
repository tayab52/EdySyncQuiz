using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Entities.Users
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserID { get; set; }

        [Required]
        [MaxLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column(TypeName = "nvarchar(255)")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "bit")]
        public bool IsActive { get; set; } = false;

        [Required]
        [Column(TypeName = "bit")]
        public bool IsDeleted { get; set; } = false;

        [Required]
        [Range(100000, 999999)]
        public long OTP { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime OTPExpiry { get; set; }

        [Column(TypeName = "nvarchar(2001)")]
        public string ProfileImage { get; set; } = "";

        public int? Age { get; set; }

        public string? Gender { get; set; }

        public string? Language { get; set; }

        public int? Level { get; set; } = 0; // Entry Level (Defined in CommonOperations)

        public List<UserInterest> Interests { get; set; } = [];
    }
}
