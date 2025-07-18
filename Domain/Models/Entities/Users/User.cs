using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.Entities.Users
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserID { get; set; }


        [Required(ErrorMessage = "Username is required")]
        [MaxLength(100, ErrorMessage = "Username's length cannnot exceed 50 characters")]
        [Column(TypeName = "nvarchar(50)")]
        public string Username { get; set; } = string.Empty;

        
        [Required(ErrorMessage = "Email is required")]
        [MaxLength(100, ErrorMessage = "Email's length cannnot exceed 100 characters")]
        [Column(TypeName = "nvarchar(100)")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Password is required")]
        [MaxLength(100, ErrorMessage = "Password's length too large")]
        [Column(TypeName = "nvarchar(255)")]
        public string Password { get; set; } = string.Empty;


        [Required]
        [MaxLength(10)]
        [Column(TypeName = "nvarchar(10)")]
        public string Role { get; set; } = "User";
    }
}
