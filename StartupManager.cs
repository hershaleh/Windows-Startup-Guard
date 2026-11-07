using Microsoft.Win32;
using System.Diagnostics;
using System.Text.Json;

namespace WindowsStartupGuard;

public sealed class StartupManager
{
    private readonly string _backupRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindowsStartupGuard", "Backups");
    private const string RegistryBackup = @"Software\WindowsStartupGuard\DisabledRegistry";

    public StartupManager() => Directory.CreateDirectory(_backupRoot);

    public List<StartupEntry> GetEntries()
    {
        var list = new List<StartupEntry>();
        ReadRunKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", StartupScope.CurrentUser, list);
        ReadRunKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", StartupScope.CurrentUser, list);
        ReadRunKey(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", StartupScope.AllUsers, list);
        ReadRunKey(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", StartupScope.AllUsers, list);
        ReadRunKey(Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", StartupScope.AllUsers, list);
        ReadRunKey(Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce", StartupScope.AllUsers, list);
        ReadFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), StartupScope.CurrentUser, list);
        ReadFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), StartupScope.AllUsers, list);
        AddMemoryUsage(list);
        AddFileMetadata(list);
        return list.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static void ReadRunKey(RegistryKey hive, string path, StartupScope scope, List<StartupEntry> list)
    {
        try
        {
            using var key = hive.OpenSubKey(path, false);
            if (key is null) return;
            foreach (var name in key.GetValueNames())
            {
                try { list.Add(new StartupEntry { Id = "reg|" + hive.Name + "|" + path + "|" + name, Name = name, Command = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString() ?? "", Source = StartupSource.Registry, Scope = scope, Location = hive.Name + "\\" + path }); }
                catch { }
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (System.Security.SecurityException) { }
        catch (IOException) { }
    }

    private static void ReadFolder(string folder, StartupScope scope, List<StartupEntry> list)
    {
        try
        {
            if (!Directory.Exists(folder)) return;
            foreach (var file in Directory.EnumerateFiles(folder))
                list.Add(new StartupEntry { Id = "file|" + file, Name = Path.GetFileNameWithoutExtension(file), Command = file, Source = StartupSource.StartupFolder, Scope = scope, Location = folder, FilePath = file });
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }

    private static string ExtractExecutable(string command)
    {
        var text = Environment.ExpandEnvironmentVariables(command.Trim());
        if (text.StartsWith('"')) { var end = text.IndexOf('"', 1); return end > 1 ? text[1..end] : text.Trim('"'); }
        var exe = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        return exe >= 0 ? text[..(exe + 4)] : text.Split(' ')[0];
    }

    private static void AddMemoryUsage(List<StartupEntry> entries)
    {
        var processes = Process.GetProcesses();
        try
        {
            var memoryByName = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (var process in processes)
            {
                try
                {
                    var name = process.ProcessName;
                    if (!memoryByName.ContainsKey(name)) memoryByName[name] = 0;
                    memoryByName[name] += process.WorkingSet64;
                }
                catch { }
            }

            foreach (var entry in entries)
            {
                try
                {
                    var name = Path.GetFileNameWithoutExtension(ExtractExecutable(entry.Command));
                    if (!string.IsNullOrWhiteSpace(name) && memoryByName.TryGetValue(name, out var bytes))
                        entry.MemoryMb = bytes / (1024 * 1024);
                }
                catch { entry.MemoryMb = 0; }
            }
        }
        finally { foreach (var process in processes) process.Dispose(); }
    }

    private static void AddFileMetadata(List<StartupEntry> entries)
    {
        foreach (var entry in entries)
        {
            try
            {
                var executable = entry.FilePath ?? ExtractExecutable(entry.Command);
                if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable)) continue;
                entry.ExecutablePath = executable;
                var info = FileVersionInfo.GetVersionInfo(executable);
                entry.Publisher = string.IsNullOrWhiteSpace(info.CompanyName) ? "Unknown" : info.CompanyName;
                entry.FileVersion = info.FileVersion ?? "";
            }
            catch { entry.Publisher = "Unavailable"; }
        }
    }

    public void Disable(StartupEntry entry)
    {
        if (entry.Source == StartupSource.StartupFolder) { DisableFile(entry); return; }
        var parts = entry.Id.Split('|', 4);
        if (parts.Length != 4) throw new InvalidOperationException("Invalid registry item.");
        if (!IsAllowedRegistryTarget(parts[1], parts[2]) || string.IsNullOrWhiteSpace(parts[3]))
            throw new InvalidOperationException("The registry startup target is not on the allowed list.");
        var hive = parts[1].Equals(Registry.CurrentUser.Name, StringComparison.OrdinalIgnoreCase) ? Registry.CurrentUser : Registry.LocalMachine;
        using var source = hive.OpenSubKey(parts[2], true) ?? throw new UnauthorizedAccessException("Cannot modify this key. Administrator rights may be required.");
        var kind = source.GetValueKind(parts[3]);
        var data = source.GetValue(parts[3], null, RegistryValueOptions.DoNotExpandEnvironmentNames) ?? throw new InvalidOperationException("The startup value no longer exists.");
        using var backup = Registry.CurrentUser.CreateSubKey(RegistryBackup, true)!;
        var backupName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(entry.Id));
        var serializedData = data is byte[] bytes ? Convert.ToBase64String(bytes) : data is string[] strings ? JsonSerializer.Serialize(strings) : data.ToString() ?? "";
        if (backup.GetValue(backupName) != null)
            throw new InvalidOperationException("A backup for this startup item already exists.");
        backup.SetValue(backupName, JsonSerializer.Serialize(new RegistryRecord(parts[1], parts[2], parts[3], kind.ToString(), serializedData, data is byte[], data is string[])), RegistryValueKind.String);
        try
        {
            source.DeleteValue(parts[3], false);
        }
        catch
        {
            backup.DeleteValue(backupName, false);
            throw;
        }
    }

    private void DisableFile(StartupEntry entry)
    {
        var file = entry.FilePath ?? throw new InvalidOperationException("Missing startup file path.");
        if (!File.Exists(file)) throw new FileNotFoundException("Startup file no longer exists.", file);
        var stored = Path.Combine(_backupRoot, Guid.NewGuid().ToString("N") + Path.GetExtension(file));
        var metadata = stored + ".json";
        File.WriteAllText(metadata, JsonSerializer.Serialize(new FileRecord(file)));
        try
        {
            File.Move(file, stored);
        }
        catch
        {
            try { File.Delete(metadata); } catch { }
            throw;
        }
    }

    public List<string> RestoreAll()
    {
        var result = new List<string>();
        using (var backup = Registry.CurrentUser.OpenSubKey(RegistryBackup, true))
        {
            if (backup != null)
                foreach (var name in backup.GetValueNames())
                    try
                    {
                        var record = JsonSerializer.Deserialize<RegistryRecord>(backup.GetValue(name)?.ToString() ?? "");
                        if (record == null || string.IsNullOrWhiteSpace(record.Name) ||
                            !IsAllowedRegistryTarget(record.Hive, record.Path) ||
                            !Enum.TryParse<RegistryValueKind>(record.Kind, out var kind) ||
                            !Enum.IsDefined(kind) || kind == RegistryValueKind.None)
                        {
                            result.Add("Registry restore skipped: invalid or unsupported backup record.");
                            continue;
                        }
                        var hive = record.Hive.Equals(Registry.CurrentUser.Name, StringComparison.OrdinalIgnoreCase) ? Registry.CurrentUser : Registry.LocalMachine;
                        using var target = hive.CreateSubKey(record.Path, true)!;
                        if (target.GetValue(record.Name, null, RegistryValueOptions.DoNotExpandEnvironmentNames) != null)
                        {
                            result.Add("Restore skipped; registry value already exists: " + record.Name);
                            continue;
                        }
                        object value = record.IsBinary ? Convert.FromBase64String(record.Data) : record.IsMultiString ? JsonSerializer.Deserialize<string[]>(record.Data) ?? [] : record.Data;
                        if (!record.IsBinary && !record.IsMultiString && kind == RegistryValueKind.DWord && int.TryParse(record.Data, out var d)) value = d;
                        if (!record.IsBinary && !record.IsMultiString && kind == RegistryValueKind.QWord && long.TryParse(record.Data, out var q)) value = q;
                        target.SetValue(record.Name, value, kind);
                        backup.DeleteValue(name, false);
                        result.Add("Restored registry item: " + record.Name);
                    }
                    catch (Exception ex) { result.Add("Registry restore error: " + ex.Message); }
        }
        string[] metadataFiles;
        try { metadataFiles = Directory.EnumerateFiles(_backupRoot, "*.json").ToArray(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            result.Add("Could not read file backups: " + ex.Message);
            return result;
        }

        foreach (var meta in metadataFiles)
            try
            {
                var record = JsonSerializer.Deserialize<FileRecord>(File.ReadAllText(meta));
                var stored = meta[..^5];
                if (record == null || !IsAllowedStartupFolderPath(record.OriginalPath) || !File.Exists(stored))
                {
                    result.Add("File restore skipped: invalid backup path or missing backup file.");
                    continue;
                }
                var destination = record.OriginalPath;
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                if (File.Exists(destination))
                {
                    var directory = Path.GetDirectoryName(destination)!;
                    var name = Path.GetFileNameWithoutExtension(destination);
                    var extension = Path.GetExtension(destination);
                    var index = 1;
                    do { destination = Path.Combine(directory, name + ".restored-" + index++ + extension); }
                    while (File.Exists(destination));
                }
                File.Move(stored, destination); File.Delete(meta);
                result.Add("Restored file: " + Path.GetFileName(destination));
            }
            catch (Exception ex) { result.Add("File restore error: " + ex.Message); }
        return result;
    }

    public void StopMatchingProcesses(StartupEntry entry)
    {
        var expectedPath = entry.ExecutablePath ?? entry.FilePath ?? ExtractExecutable(entry.Command);
        if (string.IsNullOrWhiteSpace(expectedPath))
            throw new InvalidOperationException("The executable path could not be determined safely.");

        expectedPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(expectedPath.Trim().Trim('"')));
        var name = Path.GetFileNameWithoutExtension(expectedPath);
        if (string.IsNullOrWhiteSpace(name)) return;

        var matched = 0;
        var unverifiable = 0;
        foreach (var process in Process.GetProcessesByName(name))
            using (process)
            {
                try
                {
                    var runningPath = process.MainModule?.FileName;
                    if (string.IsNullOrWhiteSpace(runningPath)) { unverifiable++; continue; }
                    if (!string.Equals(Path.GetFullPath(runningPath), expectedPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    matched++;
                    process.CloseMainWindow();
                    if (!process.WaitForExit(3000))
                        throw new InvalidOperationException("The matching application did not close after a graceful close request. It was left running to avoid losing work.");
                }
                catch (InvalidOperationException) { throw; }
                catch (Exception ex)
                {
                    unverifiable++;
                    if (matched > 0)
                        throw new InvalidOperationException("A matching process could not be checked or closed safely: " + ex.Message, ex);
                }
            }

        if (matched == 0)
        {
            var detail = unverifiable > 0 ? " Some processes could not be inspected due to Windows permissions." : "";
            throw new InvalidOperationException("No running process with the exact executable path was found." + detail);
        }
    }

    public int DisabledBackupCount()
    {
        var count = 0;
        using (var key = Registry.CurrentUser.OpenSubKey(RegistryBackup)) count += key?.GetValueNames().Length ?? 0;
        try { count += Directory.EnumerateFiles(_backupRoot, "*.json").Count(); } catch { }
        return count;
    }

    private static bool IsAllowedRegistryTarget(string hive, string path)
    {
        var validHive = hive.Equals(Registry.CurrentUser.Name, StringComparison.OrdinalIgnoreCase) ||
                        hive.Equals(Registry.LocalMachine.Name, StringComparison.OrdinalIgnoreCase);
        if (!validHive || string.IsNullOrWhiteSpace(path)) return false;

        string[] allowed =
        [
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
            @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
            @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce"
        ];
        return allowed.Contains(path, StringComparer.OrdinalIgnoreCase) &&
               !(hive.Equals(Registry.CurrentUser.Name, StringComparison.OrdinalIgnoreCase) &&
                 path.StartsWith(@"Software\WOW6432Node", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsAllowedStartupFolderPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            var fullPath = Path.GetFullPath(path);
            var folders = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
            }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(Path.GetFullPath);

            return folders.Any(folder =>
                fullPath.StartsWith(folder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase));
        }
        catch { return false; }
    }

    private sealed record RegistryRecord(string Hive, string Path, string Name, string Kind, string Data, bool IsBinary, bool IsMultiString = false);
    private sealed record FileRecord(string OriginalPath);
}
