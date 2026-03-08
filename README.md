<p align="center">
  <img src="https://img.shields.io/badge/.NET-8%2B-purple">
  <img src="https://img.shields.io/badge/analysis-Roslyn-orange">
  <img src="https://img.shields.io/badge/license-MIT-limegreen">
</p>

# dotnet-arch

> ESLint-style architecture analyzer for .NET projects

`dotnet-arch` is a lightweight CLI tool that statically analyzes C# codebases using Roslyn. It enforces clean architecture rules, detects dependency violations, and generates visual dependency graphs — all without requiring a build.

---

## Install

```bash
dotnet tool install -g dotnet-arch
```

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download) or later.

---

## Quick Start

```bash
# Analyze the current directory
dotnet-arch analyze .

# Analyze a specific project
dotnet-arch analyze ./src/MyApp

# Generate a Mermaid dependency graph
dotnet-arch graph ./src/MyApp

# Save graph to a file
dotnet-arch graph ./src/MyApp --output graph.md

# Create a config file with defaults
dotnet-arch init
```

---

## Rules

| Rule ID                    | Default  | Description                                                  |
|----------------------------|----------|--------------------------------------------------------------|
| `arch/layer-violation`     | warning  | A lower layer (e.g. Domain) references a higher one (e.g. Infrastructure) |
| `arch/circular-dependency` | error    | Two or more namespaces form a dependency cycle               |
| `arch/high-coupling`       | warning  | A type depends on more namespaces than the configured threshold |

---

## Configuration

Run `dotnet-arch init` to generate a `dotnetarch.json` in your project root:

```jsonc
{
  // Layer keywords are matched against namespace segments (case-insensitive).
  // Example: "MyApp.Services.UserService" matches the "services" keyword → application layer.
  "layers": {
    "domain":         ["models", "entities", "dto", "exceptions"],
    "application":    ["services", "handlers"],
    "infrastructure": ["repositories", "context", "dataaccess"],
    "web":            ["controllers"]
  },

  // Rule severity: "error" | "warning" | "info" | "off"
  "rules": {
    "arch/layer-violation":     "warning",
    "arch/circular-dependency": "error",
    "arch/high-coupling":       "warning"
  },

  // Numeric thresholds for rules that support them
  "thresholds": {
    "arch/high-coupling": 10
  }
}
```

The config file is discovered by walking up the directory tree from the project path — the same way ESLint resolves `.eslintrc`.

### Supported layer names

The following names and aliases are recognized out of the box:

| Layer          | Aliases                                     |
|----------------|---------------------------------------------|
| `domain`       | `core`                                      |
| `application`  | —                                           |
| `infrastructure` | `data`, `persistence`                     |
| `presentation` | `web`, `ui`, `api`                          |

---

## Ignoring paths

Create a `.archignore` file in your project root to skip directories during analysis:

```
# Auto-generated files
Migrations/
Generated/

# Test projects
Tests/
```

Lines starting with `#` are treated as comments.

---

## Exit codes

| Code | Meaning                              |
|------|--------------------------------------|
| `0`  | No violations (or only warnings)     |
| `1`  | One or more errors found             |

This makes `dotnet-arch` suitable for CI/CD pipelines:

```yaml
# GitHub Actions example
- name: Check architecture
  run: dotnet-arch analyze ./src
```

---

## Dependency graph

The `graph` command outputs a [Mermaid](https://mermaid.js.org) diagram of namespace-level dependencies:

```bash
dotnet-arch graph ./src --output graph.md
```

Paste the output into [mermaid.live](https://mermaid.live) or any Markdown renderer that supports Mermaid (GitHub, GitLab, Notion, etc.).

---

## How it works

- Parses all `.cs` files using **Roslyn** (no build required)
- Extracts type declarations and `using` directives
- Maps namespace segments to architecture layers
- Runs registered rules against the dependency graph
- Outputs ESLint-style violations grouped by rule

---

## Contributing

Contributions are welcome. To add a new rule, implement `IArchRule`:

```csharp
public sealed class MyRule : IArchRule
{
    public string RuleId => "arch/my-rule";

    public IEnumerable<AnalysisViolation> Analyze(
        IReadOnlyList<TypeInfo> types, ArchConfig config)
    {
        if (!config.IsRuleEnabled(RuleId)) yield break;

        // your logic here
    }
}
```

Then register it in `Program.cs`:

```csharp
services.AddSingleton<IArchRule, MyRule>();
```

---

## License

MIT
