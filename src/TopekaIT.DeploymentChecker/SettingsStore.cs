using System.IO;
using System.Text.Json;

namespace TopekaIT.DeploymentChecker;

public static class SettingsStore
{
    static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string AppDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TopekaIT",
        "DeploymentChecker");

    public static string SettingsPath => Path.Combine(AppDirectory, "settings.json");
    public static string HistoryPath => Path.Combine(AppDirectory, "checker-log.jsonl");
    public static string DeploymentLogPath => Path.Combine(AppDirectory, "deploy-log.txt");

    public static async Task<DeploymentSettings> LoadSettingsAsync()
    {
        if (!File.Exists(SettingsPath))
        {
            return new DeploymentSettings();
        }

        try
        {
            await using var stream = File.OpenRead(SettingsPath);
            return await JsonSerializer.DeserializeAsync<DeploymentSettings>(stream) ?? new DeploymentSettings();
        }
        catch
        {
            return new DeploymentSettings();
        }
    }

    public static async Task SaveSettingsAsync(DeploymentSettings settings)
    {
        Directory.CreateDirectory(AppDirectory);
        await using var stream = File.Create(SettingsPath);
        await JsonSerializer.SerializeAsync(stream, settings.WithoutPassword(), JsonOptions);
    }

    public static async Task AppendHistoryAsync(StatusCheckResult result)
    {
        Directory.CreateDirectory(AppDirectory);
        var json = JsonSerializer.Serialize(result, JsonOptions).ReplaceLineEndings("");
        await File.AppendAllTextAsync(HistoryPath, json + Environment.NewLine);
    }

    public static async Task<IReadOnlyList<StatusCheckResult>> LoadRecentHistoryAsync(int count = 25)
    {
        if (!File.Exists(HistoryPath))
        {
            return Array.Empty<StatusCheckResult>();
        }

        var lines = await File.ReadAllLinesAsync(HistoryPath);
        return lines
            .Reverse()
            .Take(count)
            .Select(line =>
            {
                try
                {
                    return JsonSerializer.Deserialize<StatusCheckResult>(line);
                }
                catch
                {
                    return null;
                }
            })
            .Where(result => result is not null)
            .Cast<StatusCheckResult>()
            .ToList();
    }
}
