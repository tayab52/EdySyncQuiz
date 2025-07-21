using System.Security.Cryptography;
using System.Text;

namespace CommonOperations.Encryption
{
    public class Encryption
    {
        public static string EncryptPassword(string Password)
        {
            string password = Base64Encode(Password);
            return ConvertStringToSHA256(password);
        }

        public static string Base64Encode(string password)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(password);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private static String ConvertStringToSHA256(string value)
        {
            StringBuilder sb = new StringBuilder();

            using (var hash = SHA256.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(value));

                foreach (byte b in result)
                    sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
