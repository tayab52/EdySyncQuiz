using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Entities.Users
{
    public class User
    {
        [Key]
        public Guid UserID { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(50)")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(100)")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(255)")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "bit")]
        public bool IsActive { get; set; } = false;

        [Required]
        [Column(TypeName = "bit")]
        public bool IsDeleted { get; set; } = false;

        [Column(TypeName = "bit")]
        public bool IsDataSubmitted { get; set; } = false;

        [Required]
        [Range(100000, 999999)]
        public long OTP { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime OTPExpiry { get; set; }

        [Column(TypeName = "nvarchar(2000)")]
        public string ProfileImage { get; set; } = "";

        public string ImageKey { get; set; } = "";

        public DateTime ExpiresAt { get; set; }

        [Column("Interests", TypeName = "nvarchar(MAX)")]
        public string? Interests { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? Languages { get; set; } = string.Empty;

        public string? Theme { get; set; } = "light";

        public int? Level { get; set; } = 0; // Entry Level (Defined in CommonOperations)
    }
}
