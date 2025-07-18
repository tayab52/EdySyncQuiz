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
        public dynamic Data { get; set; }

        private static ResponseVM instance = null;

        private ResponseVM() // Private constructor to prevent instantiation from outside the class
        {
        }

        public static ResponseVM Instance
        {
            get // Singleton pattern to ensure only one instance of ResponseVM is created
            {
                if (instance == null)
                {
                    instance = new ResponseVM();
                }
                return instance;
            }
        }

    }
}
