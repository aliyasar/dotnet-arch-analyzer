using System.Text.Json;

namespace DotnetArchAnalyzer.Core.Config;

public static class ArchConfigLoader
{
    /// <summary>Config file names searched in priority order.</summary>
    public static readonly string[] ConfigFileNames = ["dotnetarch.json", ".archrc.json"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Searches for dotnetarch.json (then .archrc.json) starting at projectPath and walking up.
    /// Returns (config, configPath) — configPath is null if defaults are used.
    /// </summary>
    public static (ArchConfig Config, string? ConfigPath) Load(string projectPath)
    {
        var dir = new DirectoryInfo(projectPath);
        while (dir != null)
        {
            foreach (var fileName in ConfigFileNames)
            {
                var configPath = Path.Combine(dir.FullName, fileName);
                if (!File.Exists(configPath)) continue;

                try
                {
                    var json = File.ReadAllText(configPath);
                    var loaded = JsonSerializer.Deserialize<ArchConfig>(json, JsonOptions);
                    if (loaded != null)
                        return (MergeWithDefaults(loaded), configPath);
                }
                catch { /* malformed config — try next */ }
            }
            dir = dir.Parent;
        }

        return (ArchConfig.Default, null);
    }

    /// <summary>
    /// For any layer not specified in the file, keeps the default keywords.
    /// Specified layers fully replace the default keywords for that layer.
    /// </summary>
    private static ArchConfig MergeWithDefaults(ArchConfig loaded)
    {
        var defaults = ArchConfig.Default;

        var layers = new Dictionary<string, string[]>(defaults.Layers);
        foreach (var (key, value) in loaded.Layers)
            layers[key.ToLowerInvariant()] = value.Select(k => k.ToLowerInvariant()).ToArray();

        var rules = new Dictionary<string, string>(defaults.Rules);
        foreach (var (key, value) in loaded.Rules)
            rules[key] = value;

        var thresholds = new Dictionary<string, int>(defaults.Thresholds);
        foreach (var (key, value) in loaded.Thresholds)
            thresholds[key] = value;

        return new ArchConfig { Layers = layers, Rules = rules, Thresholds = thresholds };
    }
}
