using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models.Entities.Users
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        [MaxLength(50, ErrorMessage = "Username length cannot exceed 50 characters.")]
        [Column(TypeName = "nvarchar(50)")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [MaxLength(100, ErrorMessage = "Email length cannot exceed 100 characters.")]
        [Column(TypeName = "nvarchar(100)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MaxLength(255, ErrorMessage = "Password length cannot exceed 255 characters.")]
        [Column(TypeName = "nvarchar(255)")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MaxLength(10, ErrorMessage = "Role length cannot exceed 10 characters.")]
        [Column(TypeName = "nvarchar(10)")]
        public string Role { get; set; } = "User";

        [Required]
        [Column(TypeName = "bit")]
        public bool IsActive { get; set; } = false;

        [Required]
        [Column(TypeName = "bit")]
        public bool IsDeleted { get; set; } = false;

        [Required]
        [Range(100000, 999999, ErrorMessage = "OTP must be a 6-digit number.")]
        public long OTP { get; set; }

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime OTPExpiry { get; set; }
    }
}
