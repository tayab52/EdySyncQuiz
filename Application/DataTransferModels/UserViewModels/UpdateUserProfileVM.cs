using Microsoft.AspNetCore.Http;

namespace Application.DataTransferModels.UserViewModels
{
    public class UpdateUserProfileVM
    {
        public string? Username { get; set; } = string.Empty;
        public IFormFile? Image { get; set; } 
    }
}
