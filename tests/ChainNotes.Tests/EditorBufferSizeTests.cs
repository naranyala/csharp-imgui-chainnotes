using ChainNotes.Core;

namespace ChainNotes.Tests;

public class EditorBufferSizeTests
{
    [Fact]
    public void NullText_ReturnsMinimum() =>
        Assert.Equal(EditorBufferSize.MinBytes, EditorBufferSize.RequiredBytes(null));

    [Fact]
    public void EmptyText_ReturnsMinimum() =>
        Assert.Equal(EditorBufferSize.MinBytes, EditorBufferSize.RequiredBytes(string.Empty));

    [Fact]
    public void SmallAsciiText_ReturnsMinimum() =>
        Assert.Equal(EditorBufferSize.MinBytes, EditorBufferSize.RequiredBytes("hello world"));

    [Fact]
    public void TextExactlyAtCapacityThreshold_ReturnsMinimum()
    {
        // MinBytes - SlackBytes of content fits exactly in the minimum buffer.
        var text = new string('a', (int)(EditorBufferSize.MinBytes - EditorBufferSize.SlackBytes));
        Assert.Equal(EditorBufferSize.MinBytes, EditorBufferSize.RequiredBytes(text));
    }

    [Fact]
    public void TextOneByteOverThreshold_GrowsByOne()
    {
        var text = new string('a', (int)(EditorBufferSize.MinBytes - EditorBufferSize.SlackBytes) + 1);
        Assert.Equal(EditorBufferSize.MinBytes + 1, EditorBufferSize.RequiredBytes(text));
    }

    [Fact]
    public void MultibyteCharacters_CountAsUtf8BytesNotChars()
    {
        // 'é' (U+00E9) is 2 UTF-8 bytes; char count alone would under-size the buffer.
        var text = new string('é', 40_000); // 80_000 bytes, 40_000 chars
        Assert.Equal(80_000 + EditorBufferSize.SlackBytes, EditorBufferSize.RequiredBytes(text));
    }

    [Fact]
    public void FourByteEmoji_CountAsFourBytes()
    {
        // U+1F600 needs 4 UTF-8 bytes (surrogate pair in UTF-16).
        var text = string.Concat(Enumerable.Repeat("\U0001F600", 20_000)); // 80_000 bytes
        Assert.Equal(80_000 + EditorBufferSize.SlackBytes, EditorBufferSize.RequiredBytes(text));
    }

    [Fact]
    public void LargerText_NeverGetsSmallerBuffer()
    {
        var small = EditorBufferSize.RequiredBytes(new string('a', 100));
        var large = EditorBufferSize.RequiredBytes(new string('a', 1_000_000));
        Assert.True(large > small);
    }

    [Fact]
    public void Slack_IsPositiveAndBelowMinimum()
    {
        Assert.True(EditorBufferSize.SlackBytes > 0);
        Assert.True(EditorBufferSize.SlackBytes < EditorBufferSize.MinBytes);
    }
}
