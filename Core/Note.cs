namespace ChainNotes.Core;

/// <summary>
/// A single note. <see cref="Id"/> is the stable identity used for the on-disk
/// filename and for persisting selection across restarts; <see cref="Title"/> and
/// <see cref="Body"/> are the editable contents.
/// </summary>
public sealed class Note
{
    public string Id { get; }
    public string Title { get; set; }
    public string Body { get; set; }

    public Note(string id, string title, string body)
    {
        Id = id;
        Title = title;
        Body = body;
    }
}
