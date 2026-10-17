using System.Text;

namespace ChainNotes.Core;

/// <summary>
/// Sizing rules for the editor's <c>InputTextMultiline</c> buffer.
/// Extracted from the UI so the policy is unit-testable and the widget code
/// only asks for a size. The buffer must hold the text as UTF-8 plus slack:
/// ImGui truncates whatever does not fit.
/// </summary>
public static class EditorBufferSize
{
    /// <summary>Never shrink below this, so an empty editor is cheap to type into.</summary>
    public const uint MinBytes = 64 * 1024;

    /// <summary>Headroom above the current text so a paste does not hit the cap immediately.</summary>
    public const uint SlackBytes = 8 * 1024;

    /// <summary>Buffer size in bytes required to hold <paramref name="text"/> without truncation.</summary>
    public static uint RequiredBytes(string? text)
    {
        var byteLength = text is null ? 0 : Encoding.UTF8.GetByteCount(text);
        return (uint)Math.Max(MinBytes, (long)byteLength + SlackBytes);
    }
}
