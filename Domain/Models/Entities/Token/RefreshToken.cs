using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Entities.Token
{
    public class RefreshToken
    {
        [Key]
        public Guid TokenID { get; set; } = Guid.NewGuid();
        public string Token { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public Guid UserID { get; set; }
        public Domain.Models.Entities.Users.User User { get; set; } = default!;
    }
}
