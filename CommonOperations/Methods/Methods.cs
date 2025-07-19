using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonOperations.Methods
{
    public class Methods
    {
        public static long GenerateOTP()
        {
            var random = new Random();
            return random.Next(100000, 999999);
        }

        public static bool IsValidEmailFormat(string email)
        {
            try
            {
                var mailAddress = new System.Net.Mail.MailAddress(email);
                return true; 
            }
            catch
            {
                return false;
            }
        }

        public static string GetProjectRootDirectory()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            return basePath;
        }

    }
}
