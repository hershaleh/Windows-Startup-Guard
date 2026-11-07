namespace WindowsStartupGuard;

public enum StartupSource { Registry, StartupFolder }
public enum StartupScope { CurrentUser, AllUsers }

public sealed class StartupEntry
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Command { get; init; }
    public required StartupSource Source { get; init; }
    public required StartupScope Scope { get; init; }
    public required string Location { get; init; }
    public string? FilePath { get; init; }
    public string? ExecutablePath { get; set; }
    public string Publisher { get; set; } = "—";
    public string FileVersion { get; set; } = "";
    public bool IsEnabled { get; init; } = true;
    public long MemoryMb { get; set; }
}
