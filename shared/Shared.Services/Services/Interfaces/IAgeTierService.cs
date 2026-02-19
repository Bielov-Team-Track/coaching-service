using Shared.Enums;

namespace Shared.Services.Services.Interfaces;

public interface IAgeTierService
{
    AgeTier CalculateAgeTier(DateTime dateOfBirth, string countryCode, DateTime? asOfDate = null);
    int GetConsentAge(string countryCode);
    int CalculateAge(DateTime dateOfBirth, DateTime? asOfDate = null);
    bool IsMinor(DateTime dateOfBirth, DateTime? asOfDate = null);
    bool CanHaveCredentials(DateTime dateOfBirth, DateTime? asOfDate = null);
}
