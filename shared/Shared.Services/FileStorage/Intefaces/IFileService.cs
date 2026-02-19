namespace Shared.Services.FileStorage.Intefaces
{
    public interface IFileService
    {
        Task<string> GetPresignedUploadLink(string fileName, string folder, string contentType);

        /// <summary>
        /// Moves an object from one key to another within the same bucket.
        /// </summary>
        Task MoveObjectAsync(string sourceKey, string destinationKey, string bucket);

        /// <summary>
        /// Deletes an object from the bucket.
        /// </summary>
        Task DeleteObjectAsync(string key, string bucket);

        /// <summary>
        /// Checks if an object exists in the bucket.
        /// </summary>
        Task<bool> ObjectExistsAsync(string key, string bucket);

        /// <summary>
        /// Gets the public URL for an object. Uses configured base URL (S3 or CDN).
        /// </summary>
        string GetPublicUrl(string key);
    }
}
