namespace Application.DataTransferModels.UserViewModels
{
    public class ChangePasswordVM
    {
        public int UserID { get; set; }
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
