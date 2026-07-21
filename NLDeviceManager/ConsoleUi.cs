namespace NLDeviceManager;

/// <summary>
/// Shared helpers for the console user interface (separators, banners and count summaries).
/// </summary>
internal static class ConsoleUi
{
    public const string Separator = "================================";

    public static void PrintSeparator() => Console.WriteLine(Separator);

    /// <summary>
    /// Prints a title enclosed between two separator lines.
    /// </summary>
    public static void PrintBanner(string title)
    {
        PrintSeparator();
        Console.WriteLine(title);
        PrintSeparator();
    }

    /// <summary>
    /// Prints "No {label} found." when the count is zero, otherwise "Total {label} found: {count}".
    /// </summary>
    public static void PrintCountSummary(int count, string label)
    {
        if (count == 0)
        {
            Console.WriteLine($"No {label} found.");
        }
        else
        {
            Console.WriteLine($"\nTotal {label} found: {count}");
        }
    }
}
