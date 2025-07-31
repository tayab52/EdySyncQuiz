namespace Application.DataTransferModels.UserViewModels
{
    public class UserDTO
    {
        public Guid? UserID { get; set; }
        public string? Username { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public bool? IsDataSubmitted { get; set; }
        public string? Languages { get; set; } = string.Empty;
        public string? Gender { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public int? Level { get; set; } = 0;
        public string? Interests { get; set; } = string.Empty;
        public string? Theme { get; set; } = "light";
        public string? ProfileImage { get; set; } = string.Empty;
    }
}
