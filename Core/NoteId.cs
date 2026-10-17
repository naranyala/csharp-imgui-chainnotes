using System.Text;

namespace ChainNotes.Core;

/// <summary>
/// Pure helpers for deriving filesystem-safe note ids from titles.
/// Kept side-effect free so it can be unit tested in isolation.
/// </summary>
public static class NoteId
{
    public const string Fallback = "note";

    /// <summary>
    /// Caps slugs so filenames stay well under the common 255-byte per
    /// component limit even after UTF-8 expansion and a "-NN" collision
    /// suffix (most filesystems count bytes, not chars).
    /// </summary>
    public const int MaxSlugLength = 80;

    /// <summary>
    /// Lowercases the title and collapses every run of non-alphanumeric
    /// characters into a single '-'. Returns <see cref="Fallback"/> when the
    /// title yields nothing usable (e.g. all punctuation or emoji). The result
    /// is truncated to <see cref="MaxSlugLength"/> chars and never ends in '-'.
    /// </summary>
    public static string Slugify(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Fallback;

        var sb = new StringBuilder(title.Length);
        var previousWasDash = false;

        foreach (var ch in title.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                previousWasDash = false;
            }
            else if (!previousWasDash && sb.Length > 0)
            {
                sb.Append('-');
                previousWasDash = true;
            }
        }

        var slug = sb.ToString().Trim('-');
        if (slug.Length > MaxSlugLength)
            slug = slug[..MaxSlugLength].TrimEnd('-');

        return slug.Length == 0 ? Fallback : slug;
    }

    /// <summary>
    /// Returns a slug that does not collide with any id already in
    /// <paramref name="taken"/>, appending "-2", "-3", … as needed.
    /// </summary>
    public static string Allocate(string? title, IEnumerable<string>? taken)
    {
        var baseSlug = Slugify(title);
        var used = taken is null ? new HashSet<string>(StringComparer.Ordinal) : new HashSet<string>(taken, StringComparer.Ordinal);

        if (!used.Contains(baseSlug))
            return baseSlug;

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{baseSlug}-{suffix}";
            if (!used.Contains(candidate))
                return candidate;
        }
    }

    /// <summary>
    /// True when <paramref name="id"/> is safe to combine with a directory path.
    /// Ids are always produced by <see cref="Slugify"/>/loaded from a filename,
    /// but this guards against a hand-edited or corrupted store.
    /// </summary>
    public static bool IsSafe(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        foreach (var ch in id)
        {
            if (!char.IsLetterOrDigit(ch) && ch != '-')
                return false;
        }

        return true;
    }
}
