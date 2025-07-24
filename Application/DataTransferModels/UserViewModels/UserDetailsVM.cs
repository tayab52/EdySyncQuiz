namespace Application.DataTransferModels.UserViewModels
{
    public class UserDetailsVM
    {
        public string Languages { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int Level { get; set; } = 0;
        public string Interests { get; set; } = string.Empty;
    }
}
