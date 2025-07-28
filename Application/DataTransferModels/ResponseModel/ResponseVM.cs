namespace Application.DataTransferModels.ResponseModel
{
    public sealed class ResponseVM
    {
        public int StatusCode { get; set; }
        public string? ResponseMessage { get; set; } = "";
        public string? ErrorMessage { get; set; } = "";
        public dynamic? Data { get; set; } = null;

        private static ResponseVM? instance = null;

        private ResponseVM()
        {
        }

        public static ResponseVM Instance
        {
            get
            {
                {
                    instance = new ResponseVM();
                }
                return instance;
            }
        }
    }
}
