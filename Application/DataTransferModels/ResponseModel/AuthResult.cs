namespace Application.DataTransferModels.ResponseModel
{
    public class AuthResult
    {
        public string AccessToken { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
    }
}
