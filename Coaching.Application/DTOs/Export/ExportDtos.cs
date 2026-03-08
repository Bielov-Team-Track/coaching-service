namespace Coaching.Application.DTOs.Export;

public enum ExportFormat
{
    Csv,
    Pdf
}

public record ExportEvaluationRequest
{
    public Guid SessionId { get; set; }
    public ExportFormat Format { get; set; } = ExportFormat.Csv;
    public bool IncludeMetricDetails { get; set; } = true;
    public bool IncludeCoachNotes { get; set; } = false;
}

public record ExportPlayerHistoryRequest
{
    public Guid PlayerId { get; set; }
    public ExportFormat Format { get; set; } = ExportFormat.Csv;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class ExportResult
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
