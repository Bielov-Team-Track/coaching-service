using Shared.Models;

namespace Shared.Services.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(AuditEntryDto entry);
}
