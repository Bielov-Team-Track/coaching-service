namespace Shared.Data;

public static class ConsentAgeMap
{
    private static readonly Dictionary<string, int> CountryConsentAges = new(StringComparer.OrdinalIgnoreCase)
    {
        // 13
        ["BE"] = 13, ["DK"] = 13, ["EE"] = 13, ["FI"] = 13,
        ["LV"] = 13, ["MT"] = 13, ["PT"] = 13, ["SE"] = 13, ["GB"] = 13,
        // 14
        ["AT"] = 14, ["BG"] = 14, ["CY"] = 14, ["IT"] = 14,
        ["LT"] = 14, ["ES"] = 14,
        // 15
        ["CZ"] = 15, ["FR"] = 15, ["GR"] = 15, ["SI"] = 15,
        // 16 (GDPR default)
        ["HR"] = 16, ["DE"] = 16, ["HU"] = 16, ["IE"] = 16,
        ["LU"] = 16, ["NL"] = 16, ["PL"] = 16, ["RO"] = 16, ["SK"] = 16,
        // US (COPPA)
        ["US"] = 13,
    };

    public static int GetConsentAge(string countryCode)
    {
        return CountryConsentAges.TryGetValue(countryCode, out var age) ? age : 16;
    }
}
