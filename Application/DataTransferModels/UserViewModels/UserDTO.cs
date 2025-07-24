using Domain.Models.Entities.Users;

namespace Application.DataTransferModels.UserViewModels
{
    public class UserDTO
    {
        public int? UserID { get; set; }
        public string? Username { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public bool? IsDataSubmitted { get; set; }
        public string? Language { get; set; } = string.Empty;
        public string? Gender { get; set; } = string.Empty;
        public int? Age { get; set; }
        public int? Level { get; set; } = 0;
        public List<UserInterest> Interests { get; set; } = [];
    }
}
