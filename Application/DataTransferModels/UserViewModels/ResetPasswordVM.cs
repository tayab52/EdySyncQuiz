namespace Application.DataTransferModels.UserViewModels
{
    public class ResetPasswordVM
    {
        public string Email { get; set; } = string.Empty;
        public long OTP { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}
