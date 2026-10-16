using ChainNotes.App;

namespace ChainNotes;

public static class Program
{
    public static void Main(string[] args)
    {
        using var application = new Application();
        application.Run(args);
    }
}
