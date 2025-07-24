using Amazon.S3;
using Amazon.S3.Model;
using Amazon;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Infrastructure.Services.Wasabi
{
    public class WasabiService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly int _expirySeconds;

        public WasabiService(IConfiguration config)
        {
            var settings = config.GetSection("WasabiSettings");
            var accessKey = settings["AccessKey"];
            var secretKey = settings["SecretKey"];
            _bucketName = settings["BucketName"]!;
            _expirySeconds = int.Parse(settings["URLExpirySeconds"]!);

            var s3Config = new AmazonS3Config
            {
                ServiceURL = settings["ServiceUrl"],
                ForcePathStyle = true,
            };

            _s3Client = new AmazonS3Client(accessKey, secretKey, s3Config);
        }

        public async Task<string> UploadBase64ImageAsync(string base64String, string key)
        {
            try
            {
                var base64Data = base64String.Contains(',')
                    ? base64String.Split(',')[1]
                    : base64String;

                base64Data = base64Data.Replace(" ", "+").Trim();

                var bytes = Convert.FromBase64String(base64Data);
                Console.WriteLine($"Decoded Bytes Length: {bytes.Length}");
                using var stream = new MemoryStream(bytes);
                stream.Position = 0;

                Console.WriteLine($"Bucket: {_bucketName}");
                Console.WriteLine($"Key: {key}");

                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = stream,
                    AutoCloseStream = false,
                    UseChunkEncoding = false
                };


                var response = await _s3Client.PutObjectAsync(putRequest);
                Console.WriteLine($"Uploaded to Wasabi. ETag: {response.ETag}");

                return key;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Wasabi Upload Failed:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public string GeneratePresignedUrl(string key)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddSeconds(_expirySeconds)
            };
            return _s3Client.GetPreSignedURL(request);
        }
    }
}
