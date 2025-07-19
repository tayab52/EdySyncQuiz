namespace Application.DataTransferModels.UserViewModels
{
    public class UserDTO
    {
        public int? UserID { get; set; }
        public string? Username { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public long? OTP { get; set; }

    }
}
