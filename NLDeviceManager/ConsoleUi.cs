using System.Text;

namespace NLDeviceManager;

/// <summary>
/// Shared output-formatting helpers used by both the console entrypoint and the
/// text rendered by <see cref="DeviceManagerService"/>.
/// </summary>
internal static class ConsoleUi
{
    /// <summary>
    /// The horizontal rule used as a section separator throughout the UI.
    /// </summary>
    public const string Separator = "================================";

    /// <summary>
    /// Appends a device/printer count summary: a leading blank line followed by
    /// "No {label} found." when <paramref name="count"/> is zero, otherwise
    /// "Total {label} found: {count}" (preceded by its own blank line).
    /// </summary>
    public static void AppendCountSummary(StringBuilder builder, int count, string label)
    {
        builder.AppendLine();
        if (count == 0)
        {
            builder.Append($"No {label} found.");
        }
        else
        {
            builder.AppendLine();
            builder.Append($"Total {label} found: {count}");
        }
    }
}
