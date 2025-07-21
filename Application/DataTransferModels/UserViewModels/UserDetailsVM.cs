namespace Application.DataTransferModels.UserViewModels
{
    public class UserDetailsVM
    {
        public string Language { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public int Age { get; set; }
        public int Level { get; set; } = 0;
        public List<string> Interests { get; set; } = [];
    }
}
