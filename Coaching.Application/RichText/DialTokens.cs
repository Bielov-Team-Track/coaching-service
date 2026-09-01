using System.Text.RegularExpressions;
using Coaching.Domain.Models.Drills;

namespace Coaching.Application.RichText;

/// <summary>
/// A drill's instructions carry its dials as <c>{name}</c> tokens. The client does the splicing —
/// promoting a word to a dial, renaming one, putting the words back — and sends the rewritten
/// prose along with the operation. The server's job is to check the two halves still describe
/// the same drill: every token has a dial, and every dial has a token.
///
/// Without that check the two drift silently. A promote whose splice missed leaves a dial nothing
/// reads; a delete whose splice missed leaves a token nothing fills, and the coach sees a literal
/// "{tempo}" in the middle of a sentence.
/// </summary>
public static class DialTokens
{
    /// <summary>
    /// Plural markers (<c>~s</c>, <c>~es</c>) sit outside the braces, so the token pattern is
    /// unaffected by them.
    /// </summary>
    private static readonly Regex Token = new(@"\{([A-Za-z][A-Za-z0-9]*)\}", RegexOptions.Compiled);

    /// <summary>
    /// A name has to survive being a JSON object key. The API serializes in camelCase and its
    /// resolver processes dictionary keys too, so a dial written as "Reps" comes back as "reps"
    /// and the client's splice on {Reps} finds nothing. A name that already starts lowercase is
    /// returned untouched, which is why the grammar starts there rather than at any letter.
    /// The token scan still accepts a leading capital, so a mis-cased token in the prose is
    /// reported as unknown instead of passing silently.
    /// </summary>
    private static readonly Regex NamePattern = new(@"^[a-z][A-Za-z0-9]*$", RegexOptions.Compiled);

    /// <summary>A dial's name has to be a token the prose can carry, and short enough to store.</summary>
    public static bool IsValidName(string? name) =>
        !string.IsNullOrEmpty(name)
        && name.Length <= DrillDial.NameMaxLength
        && NamePattern.IsMatch(name);

    /// <summary>Every distinct token in the lines, in the order it first appears.</summary>
    public static IReadOnlyList<string> Scan(IEnumerable<string> lines)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var found = new List<string>();

        foreach (var line in lines)
        {
            foreach (Match match in Token.Matches(line ?? string.Empty))
            {
                var name = match.Groups[1].Value;
                if (seen.Add(name)) found.Add(name);
            }
        }

        return found;
    }

    /// <summary>
    /// The names the prose mentions but no dial defines, and the dials no prose mentions —
    /// either one means the splice and the dial list disagree.
    /// </summary>
    public static (IReadOnlyList<string> Unknown, IReadOnlyList<string> Unused) Reconcile(
        IEnumerable<string> lines, IEnumerable<string> dialNames)
    {
        var tokens = Scan(lines);
        var dials = dialNames.ToList();

        return (
            tokens.Where(t => !dials.Contains(t, StringComparer.Ordinal)).ToList(),
            dials.Where(d => !tokens.Contains(d, StringComparer.Ordinal)).ToList());
    }
}
