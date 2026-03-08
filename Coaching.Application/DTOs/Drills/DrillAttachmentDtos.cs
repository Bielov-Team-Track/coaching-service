using Coaching.Domain.Enums;

namespace Coaching.Application.DTOs.Drills;

public class DrillAttachmentDto
{
    public Guid Id { get; set; }
    public Guid DrillId { get; set; }
    public required string FileName { get; set; }
    public required string FileUrl { get; set; }
    public DrillAttachmentType FileType { get; set; }
    public long FileSize { get; set; }
    public int Order { get; set; }
}

public record CreateDrillAttachmentDto(
    string FileName,
    string FileUrl,
    DrillAttachmentType FileType,
    long FileSize
);

public class DrillAttachmentUploadRequestDto
{
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required long FileSize { get; set; }
}

public class DrillAttachmentUploadResponseDto
{
    public string UploadUrl { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
}
