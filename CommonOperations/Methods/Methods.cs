using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text.RegularExpressions;

namespace CommonOperations.Methods
{
    public class Methods
    {

        public static string CleanJsonResponse(string? jsonResponse)
        {
            if (string.IsNullOrEmpty(jsonResponse))
                return "[]";

            var cleaned = jsonResponse.Trim().Trim('`');

            if (cleaned.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(4).Trim();
            }

            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(7).Trim();
            }

            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3).Trim();
            }

            return cleaned;
        }
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

        public static string GetConnectionString()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .Build();

            return configuration.GetConnectionString("ConnectionString")!;
        }

        public static async Task<IEnumerable<dynamic>> ExecuteStoredProceduresList(string storedProcedureName, DynamicParameters parameters)
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            var results = await connection.QueryAsync<dynamic>(
                storedProcedureName,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return results.Any() ? results : Enumerable.Empty<dynamic>();
        }

        public static string GetProjectRootDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string GetImageExtension(string base64String)
        {
            string? mimeType = null;

            var match = Regex.Match(base64String, @"data:(?<mime>[^;]+);base64,");
            if (match.Success)
            {
                mimeType = match.Groups["mime"].Value;
                base64String = base64String.Substring(match.Value.Length);
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64String);
            }
            catch
            {
                return "invalid_base64";
            }

            if (!string.IsNullOrEmpty(mimeType))
            {
                return MimeTypeToExtension(mimeType);
            }

            return GetImageExtensionFromBytes(bytes);
        }

        private static string MimeTypeToExtension(string mime)
        {
            return mime.ToLower() switch
            {
                "image/jpeg" => "jpg",
                "image/png" => "png",
                "image/gif" => "gif",
                "image/bmp" => "bmp",
                "image/webp" => "webp",
                _ => "unknown"
            };
        }



        private static string GetImageExtensionFromBytes(byte[] bytes)
        {
            if (bytes.Length < 4) return "unknown";

            if (bytes[0] == 0xFF && bytes[1] == 0xD8) return "jpg";
            if (bytes[0] == 0x89 && bytes[1] == 0x50) return "png";
            if (bytes[0] == 0x47 && bytes[1] == 0x49) return "gif";
            if (bytes[0] == 0x42 && bytes[1] == 0x4D) return "bmp";
            if (bytes[0] == 0x52 && bytes[1] == 0x49 &&
                bytes[2] == 0x46 && bytes[3] == 0x46) return "webp";

            return "unknown";
        }



    }
}
