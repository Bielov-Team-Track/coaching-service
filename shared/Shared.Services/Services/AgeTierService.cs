using Shared.Data;
using Shared.Enums;
using Shared.Services.Services.Interfaces;

namespace Shared.Services.Services;

public class AgeTierService : IAgeTierService
{
    public AgeTier CalculateAgeTier(DateTime dateOfBirth, string countryCode, DateTime? asOfDate = null)
    {
        var age = CalculateAge(dateOfBirth, asOfDate);
        var consentAge = ConsentAgeMap.GetConsentAge(countryCode);

        if (age >= 18) return AgeTier.Adult;
        if (age >= consentAge) return AgeTier.AboveConsentAge;
        if (age >= 15) return AgeTier.Below15To17;
        if (age >= 13) return AgeTier.Below13To14;
        return AgeTier.Under13;
    }

    public int GetConsentAge(string countryCode) => ConsentAgeMap.GetConsentAge(countryCode);

    public int CalculateAge(DateTime dateOfBirth, DateTime? asOfDate = null)
    {
        var today = asOfDate?.Date ?? DateTime.UtcNow.Date;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }

    public bool IsMinor(DateTime dateOfBirth, DateTime? asOfDate = null)
        => CalculateAge(dateOfBirth, asOfDate) < 18;

    public bool CanHaveCredentials(DateTime dateOfBirth, DateTime? asOfDate = null)
        => CalculateAge(dateOfBirth, asOfDate) >= 13;
}
