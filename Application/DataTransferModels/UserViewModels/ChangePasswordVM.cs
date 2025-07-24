namespace Application.DataTransferModels.UserViewModels
{
    public class ChangePasswordVM
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
