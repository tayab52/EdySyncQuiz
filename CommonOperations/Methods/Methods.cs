using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;


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

        public string GetProjectRootDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
    }
}
