namespace Shared.Enums;

[Flags]
public enum GuardianPermission
{
    None     = 0,
    View     = 1,
    RSVP     = 2,
    Register = 4,
    Message  = 8,
    Pay      = 16,
    Admin    = 32,
}
