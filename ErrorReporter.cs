using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WindowsStartupGuard;

internal static class ErrorReporter
{
    private static readonly string LogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindowsStartupGuard", "Logs");
    private static readonly string LogPath = Path.Combine(LogDirectory, "application.log");

    public static void LogException(Exception exception, string source)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var entry = new StringBuilder()
                .AppendLine("============================================================")
                .AppendLine(DateTimeOffset.Now.ToString("O"))
                .AppendLine("Source: " + source)
                .AppendLine("Exception: " + exception.GetType().FullName)
                .AppendLine("Message: " + exception.Message)
                .AppendLine(exception.ToString()).ToString();
            File.AppendAllText(LogPath, entry, new UTF8Encoding(false));
        }
        catch { }
    }

    public static string CreateIssueUrl(bool hebrew)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        var log = ReadRecentLog();
        var title = hebrew ? "[בעיה] דיווח תקלה מ-Windows Startup Guard" : "[Bug] Windows Startup Guard problem report";
        var body = new StringBuilder()
            .AppendLine(hebrew ? "## תיאור התקלה" : "## Problem description")
            .AppendLine(hebrew ? "נא לתאר מה קרה ומה ציפית שיקרה." : "Please describe what happened and what you expected.")
            .AppendLine().AppendLine("## Environment")
            .AppendLine("- App version: " + version)
            .AppendLine("- OS: " + RuntimeInformation.OSDescription)
            .AppendLine("- Architecture: " + RuntimeInformation.OSArchitecture)
            .AppendLine("- .NET: " + Environment.Version)
            .AppendLine("- Time: " + DateTimeOffset.Now.ToString("O"))
            .AppendLine().AppendLine("## Recent application log")
            .AppendLine("--- LOG START ---")
            .AppendLine(log.Length == 0 ? (hebrew ? "לא נרשמו שגיאות באפליקציה." : "No application errors were logged.") : log)
            .AppendLine("--- LOG END ---").AppendLine()
            .AppendLine(hebrew
                ? "יש לבדוק את הלוג ולמחוק מידע אישי אם מופיע. הדיווח יישלח רק לאחר לחיצה על Submit ב-GitHub."
                : "Review the log and remove personal information. Nothing is sent until you click Submit on GitHub.")
            .ToString();

        var url = "https://github.com/hershaleh/Windows-Startup-Guard/issues/new?title=" + Uri.EscapeDataString(title) + "&body=" + Uri.EscapeDataString(body);
        if (url.Length <= 7500) return url;
        var shortened = body[..Math.Min(body.Length, 1800)];
        return "https://github.com/hershaleh/Windows-Startup-Guard/issues/new?title=" + Uri.EscapeDataString(title) + "&body=" + Uri.EscapeDataString(shortened);
    }

    private static string ReadRecentLog()
    {
        try
        {
            var path = Path.Combine(LogDirectory, "application.log");
            if (!File.Exists(path)) return string.Empty;
            var text = string.Join(Environment.NewLine, File.ReadAllLines(path).TakeLast(100));
            text = text.Replace(Environment.UserName, "[user]", StringComparison.OrdinalIgnoreCase);
            text = Regex.Replace(text, @"(?i)(authorization\s*[:=]\s*bearer\s+)\S+", "$1[redacted]");
            text = Regex.Replace(text, @"(?i)(token|password|secret|api[_-]?key)(\s*[:=]\s*)[^\s;]+", "$1$2[redacted]");
            return text.Length > 1200 ? text[^1200..] : text;
        }
        catch { return "Log file could not be read."; }
    }
}
