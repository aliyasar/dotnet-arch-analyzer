using DotnetArchAnalyzer.Core.Config;
using DotnetArchAnalyzer.Core.Models;
using DotnetArchAnalyzer.Core.Rules;

namespace DotnetArchAnalyzer.Rules;

/// <summary>
/// Detects violations of layered architecture principles.
///
/// Layer hierarchy (inner → outer):
///   Domain(0) → Application(1) → Infrastructure(2) / Presentation(3)
///
/// Layer keywords are loaded from dotnetarch.json (or built-in defaults).
/// Forbidden dependency directions and their default severities:
///   Domain → any outer layer                    [Error]
///   Application → Presentation                  [Error]
///   Infrastructure → Presentation               [Error]
///   Application → Infrastructure                [Warning]  (prefer interfaces)
///   Presentation → Infrastructure (layer skip)  [Warning]
/// </summary>
public sealed class LayerViolationRule : IArchRule
{
    public string RuleId => "arch/layer-violation";

    // Layer indices: lower = more inner. Aliases allow custom names in dotnetarch.json.
    private static readonly Dictionary<string, int> LayerIndex = new()
    {
        ["domain"]         = 0,
        ["application"]    = 1,
        ["infrastructure"] = 2,
        ["presentation"]   = 3,
        // Common aliases — map to the same layer index
        ["web"]            = 3,
        ["ui"]             = 3,
        ["api"]            = 3,
        ["data"]           = 2,
        ["persistence"]    = 2,
        ["core"]           = 0,
    };

    private static readonly Dictionary<int, string> DefaultDisplayNames = new()
    {
        [0] = "Domain", [1] = "Application", [2] = "Infrastructure", [3] = "Presentation",
    };

    // Forbidden pairs with default severity. Config can override the whole rule's severity.
    private static readonly Dictionary<(int From, int To), ViolationSeverity> ForbiddenPairs = new()
    {
        { (0, 1), ViolationSeverity.Error },   // Domain → Application
        { (0, 2), ViolationSeverity.Error },   // Domain → Infrastructure
        { (0, 3), ViolationSeverity.Error },   // Domain → Presentation/Web
        { (1, 3), ViolationSeverity.Error },   // Application → Presentation/Web
        { (2, 3), ViolationSeverity.Error },   // Infrastructure → Presentation/Web
        { (1, 2), ViolationSeverity.Warning }, // Application → Infrastructure (prefer interfaces)
        { (3, 2), ViolationSeverity.Warning }, // Presentation → Infrastructure (skipping Application)
    };

    public IEnumerable<AnalysisViolation> Analyze(IReadOnlyList<TypeInfo> types, IReadOnlyList<MethodInfo> methods, ArchConfig config)
    {
        if (!config.IsRuleEnabled(RuleId)) yield break;

        var severityOverride = config.GetSeverityOverride(RuleId);
        var (layerMap, displayNames) = BuildLayerMap(config);

        foreach (var type in types)
        {
            var typeLayer = DetectLayer(type.Namespace, layerMap);
            if (typeLayer is null) continue;

            foreach (var refNs in type.ReferencedNamespaces)
            {
                if (refNs == type.Namespace) continue;

                var refLayer = DetectLayer(refNs, layerMap);
                if (refLayer is null) continue;

                var pair = (typeLayer.Value, refLayer.Value);
                if (!ForbiddenPairs.TryGetValue(pair, out var defaultSeverity)) continue;

                yield return new AnalysisViolation(
                    severityOverride ?? defaultSeverity,
                    RuleId,
                    $"{type.Name} ({displayNames[typeLayer.Value]} layer) references {refNs} ({displayNames[refLayer.Value]} layer)",
                    type.FilePath,
                    type.Line);
            }
        }
    }

    /// <summary>
    /// Builds keyword→layerIndex map and layerIndex→displayName map from config.
    /// Display names come from config keys (e.g. "web" → "Web"), falling back to defaults.
    /// </summary>
    private static (Dictionary<string, int> LayerMap, Dictionary<int, string> DisplayNames) BuildLayerMap(ArchConfig config)
    {
        var layerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var displayNames = new Dictionary<int, string>(DefaultDisplayNames);

        foreach (var (layerName, keywords) in config.Layers)
        {
            var key = layerName.ToLowerInvariant();
            if (!LayerIndex.TryGetValue(key, out var idx)) continue;

            foreach (var keyword in keywords)
                layerMap[keyword.ToLowerInvariant()] = idx;

            // Use the config's key as display name (e.g. "web" → "Web")
            displayNames[idx] = char.ToUpperInvariant(layerName[0]) + layerName[1..].ToLowerInvariant();
        }

        return (layerMap, displayNames);
    }

    private static int? DetectLayer(string namespaceName, Dictionary<string, int> layerMap)
    {
        if (string.IsNullOrEmpty(namespaceName)) return null;

        foreach (var segment in namespaceName.ToLowerInvariant().Split('.').Reverse())
        {
            if (layerMap.TryGetValue(segment, out var layer))
                return layer;
        }

        return null;
    }
}
