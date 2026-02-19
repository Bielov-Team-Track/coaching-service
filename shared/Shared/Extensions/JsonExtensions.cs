using Newtonsoft.Json.Linq;
namespace Shared.Extensions;

public static class JsonExtensions
{
    public static Dictionary<string, object>? ToFlatDictionary(this string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        var jObject = JObject.Parse(json);
        return jObject.Descendants()
            .OfType<JValue>()
            .ToDictionary(
                jv => jv.Path,
                jv => jv.Value ?? (object)string.Empty
            );
    }
}

