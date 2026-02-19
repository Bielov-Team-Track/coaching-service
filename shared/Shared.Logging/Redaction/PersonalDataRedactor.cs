using Microsoft.Extensions.Compliance.Redaction;

namespace Shared.Logging.Redaction;

public class PersonalDataRedactor : Redactor
{
    private const int VisibleChars = 3;
    private const int MaskedLength = 8;
    private const char MaskChar = '*';

    public override int GetRedactedLength(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty)
            return 0;

        // Check if it looks like an email
        int atIndex = input.IndexOf('@');
        if (atIndex > 0)
        {
            // For emails: first 3 chars + mask + @ + domain
            var domainLength = input.Length - atIndex; // includes @
            return VisibleChars + MaskedLength + domainLength;
        }

        return input.Length <= VisibleChars
            ? MaskedLength
            : VisibleChars + MaskedLength;
    }

    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        if (source.IsEmpty || destination.IsEmpty)
            return 0;

        // Check if it's an email
        int atIndex = -1;
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == '@')
            {
                atIndex = i;
                break;
            }
        }

        if (atIndex > 0)
        {
            return RedactEmail(source, destination, atIndex);
        }

        // Regular PII redaction
        return RedactRegular(source, destination);
    }

    private int RedactEmail(ReadOnlySpan<char> source, Span<char> destination, int atIndex)
    {
        int written = 0;

        // Write first 3 chars of email
        int visibleLength = Math.Min(Math.Min(VisibleChars, atIndex), destination.Length);
        for (int i = 0; i < visibleLength; i++)
        {
            destination[written++] = source[i];
        }

        // Write fixed mask
        int maskToWrite = Math.Min(MaskedLength, destination.Length - written);
        for (int i = 0; i < maskToWrite; i++)
        {
            destination[written++] = MaskChar;
        }

        // Write domain part (including @)
        int domainLength = source.Length - atIndex;
        int domainToWrite = Math.Min(domainLength, destination.Length - written);
        for (int i = 0; i < domainToWrite; i++)
        {
            destination[written++] = source[atIndex + i];
        }

        return written;
    }

    private int RedactRegular(ReadOnlySpan<char> source, Span<char> destination)
    {
        if (source.Length <= VisibleChars)
        {
            // All masked for short values
            int maskLength = Math.Min(MaskedLength, destination.Length);
            for (int i = 0; i < maskLength; i++)
            {
                destination[i] = MaskChar;
            }
            return maskLength;
        }

        // Copy visible part
        int written = 0;
        int visibleLength = Math.Min(VisibleChars, destination.Length);
        for (int i = 0; i < visibleLength; i++)
        {
            destination[written++] = source[i];
        }

        // Add mask
        int maskToAdd = Math.Min(MaskedLength, destination.Length - written);
        for (int i = 0; i < maskToAdd; i++)
        {
            destination[written++] = MaskChar;
        }

        return written;
    }
}