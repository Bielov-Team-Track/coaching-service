namespace Coaching.Domain.Enums;

/// <summary>
/// What a coach is allowed to change about one word in a drill's instructions. A Number is
/// counted, a Text is written, a Toggle is on or off — and a Toggle is not a Text that happens
/// to say "yes", because the two states carry whole sentences of their own.
/// </summary>
public enum DialKind
{
    Number = 0,
    Text = 1,
    Toggle = 2,
}
