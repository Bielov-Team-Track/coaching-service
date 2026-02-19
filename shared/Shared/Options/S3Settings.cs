namespace Shared.Options
{
    public class S3Settings
    {
        public required string Bucket { get; set; }
        /// <summary>
        /// Base URL for public file access. Can be direct S3 URL or CDN URL.
        /// Examples: "https://bucket.s3.us-east-1.amazonaws.com" or "https://cdn.example.com"
        /// </summary>
        public required string PublicBaseUrl { get; set; }
        public int PresignedUrlExpiryMinutes { get; set; }
    }
}
