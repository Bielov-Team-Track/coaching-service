namespace Coaching.Application.Interfaces.Services;

public interface IFeedbackMediaUrlSigner
{
    /// <summary>
    /// Returns a presigned GET URL for media stored in our bucket; any other
    /// URL (external links, empty values) is returned unchanged.
    /// </summary>
    string SignReadUrl(string url);
}
