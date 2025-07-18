using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferModels.ResponseModel
{
    public class ResponseVM
    {
        public int StatusCode { get; set; }
        public string? ResponseMessage { get; set; } = "";
        public string? ErrorMessage { get; set; } = "";
        public dynamic Data { get; set; } = null;

        public ResponseVM() 
        {
        }

        public ResponseVM(int statusCode, string? responseMessage = "", string? errorMessage = "", dynamic data = null)
        {
            StatusCode = statusCode;
            ResponseMessage = responseMessage;
            ErrorMessage = errorMessage;
            Data = data;
        }
    }
}
