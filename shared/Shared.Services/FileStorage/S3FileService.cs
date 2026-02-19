using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Shared.Options;
using Shared.Services.FileStorage.Intefaces;

namespace Shared.Services.FileStorage
{
    public class S3FileService : IFileService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly S3Settings _s3Settings;

        public S3FileService(IAmazonS3 s3Client, IOptions<S3Settings> s3Settings)
        {
            _s3Client = s3Client;
            _s3Settings = s3Settings.Value;
        }

        public async Task<string> GetPresignedUploadLink(string fileName, string folder, string contentType)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = folder,
                Key = fileName,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(_s3Settings.PresignedUrlExpiryMinutes),
                ContentType = contentType
            };

            return await _s3Client.GetPreSignedURLAsync(request);
        }

        public async Task MoveObjectAsync(string sourceKey, string destinationKey, string bucket)
        {
            // Copy to new location
            var copyRequest = new CopyObjectRequest
            {
                SourceBucket = bucket,
                SourceKey = sourceKey,
                DestinationBucket = bucket,
                DestinationKey = destinationKey
            };
            await _s3Client.CopyObjectAsync(copyRequest);

            // Delete from old location
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucket,
                Key = sourceKey
            };
            await _s3Client.DeleteObjectAsync(deleteRequest);
        }

        public async Task DeleteObjectAsync(string key, string bucket)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bucket,
                Key = key
            };
            await _s3Client.DeleteObjectAsync(request);
        }

        public async Task<bool> ObjectExistsAsync(string key, string bucket)
        {
            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = bucket,
                    Key = key
                };
                await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public string GetPublicUrl(string key)
        {
            return $"{_s3Settings.PublicBaseUrl.TrimEnd('/')}/{key.TrimStart('/')}";
        }
    }
}
