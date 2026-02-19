using System.Text.Json.Serialization;

namespace Shared.DTOs.Errors;

public class FieldError
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    public FieldError() { }

    public FieldError(string field, string code, string message)
    {
        Field = field;
        Code = code;
        Message = message;
    }
}
