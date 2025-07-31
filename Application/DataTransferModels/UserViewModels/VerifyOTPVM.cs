namespace Application.DataTransferModels.UserViewModels
{
    public class VerifyOTPVM
    {
        public string Email { get; set; } = string.Empty;
        public long OTP { get; set; } = 0;

    }
}
