using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Models.BaseEntities;

namespace Domain.Models.Entities.Users
{
    public class User : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long UserID { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(50)")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(100)")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(255)")]
        public string Password { get; set; } = string.Empty;    
        public bool IsActive { get; set; } = true;      

        public bool IsDataSubmitted { get; set; } = false;

        [Required]
        [Range(100000, 999999)]
        public long OTP { get; set; }
        public DateTime OTPExpiry { get; set; }

        [Column(TypeName = "nvarchar(2000)")]
        public string ProfileImage { get; set; } = "";
        [Column(TypeName = "nvarchar(2000)")]
        public string ImageKey { get; set; } = "";

        public DateTime ExpiresAt { get; set; }

        [Column("Interests", TypeName = "nvarchar(MAX)")]
        public string? Interests { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }
        [Column(TypeName = "nvarchar(200)")]
        public string? Gender { get; set; }
        [Column(TypeName = "nvarchar(200)")]
        public string? Languages { get; set; } = string.Empty;
        [Column(TypeName = "nvarchar(2000)")]
        public string? Theme { get; set; } = "light";

        public int? Level { get; set; } = 0; // Entry Level (Defined in CommonOperations)
    }
}
