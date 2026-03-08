<p align="center">
  <img src="assets/banner.png" width="100%">
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8%2B-purple">
  <img src="https://img.shields.io/badge/analysis-Roslyn-orange">
  <img src="https://img.shields.io/badge/license-MIT-limegreen">
</p>

# dotnet-arch

> ESLint-style architecture analyzer for .NET projects

`dotnet-arch` statically analyzes C# codebases using Roslyn. It enforces architecture rules, detects code quality issues, and generates dependency graphs — without requiring a build.

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

# List all active rules and their thresholds
dotnet-arch rules .

# Generate a Mermaid dependency graph
dotnet-arch graph ./src/MyApp

# Create a config file
dotnet-arch init .
```

---

## Output

```
Architecture Analysis  Grade: A  91/100
────────────────────────────────────────────────────────────────
       Types 28
     Methods 46
Dependencies 22
  Violations 3

complexity/method-length  (1 warning)
────────────────────────────────────────────────────────────────
  Parsing\RoslynParser.cs
     27  ⚠  RoslynParser.ParseFile is 83 lines long (threshold: 60)

Rule summary
────────────────────────────────────────────────────────────────
  complexity/method-length          1 warning

✖ 1 problem (0 errors, 1 warning)
```

Violations are grouped by rule. Each violation shows the file, line number, and a short description.

---

## Rules

### Architecture

| Rule                       | Default  | Description                                              |
|----------------------------|----------|----------------------------------------------------------|
| `arch/layer-violation`     | warning  | A lower layer (e.g. Domain) references a higher one (e.g. Infrastructure) |
| `arch/circular-dependency` | error    | Two or more namespaces form a dependency cycle           |
| `arch/high-coupling`       | warning  | A type depends on too many namespaces                    |

### Style

| Rule                       | Default  | Description                                              |
|----------------------------|----------|----------------------------------------------------------|
| `style/interface-prefix`   | warning  | Interface names should start with `I`                    |
| `style/async-suffix`       | warning  | Async method names should end with `Async`               |
| `style/no-empty-catch`     | warning  | Empty catch blocks silently swallow exceptions           |
| `style/namespace-match`    | warning  | Namespace should match the folder it lives in            |

### Complexity

| Rule                         | Default  | Threshold | Description                              |
|------------------------------|----------|-----------|------------------------------------------|
| `complexity/method-length`   | warning  | 40 lines  | Method is too long                       |
| `complexity/class-length`    | warning  | 300 lines | Class is too long                        |
| `complexity/parameter-count` | warning  | 5         | Method has too many parameters           |
| `complexity/cyclomatic`      | warning  | 10        | Too many decision paths (if/for/catch…)  |
| `complexity/nesting-depth`   | warning  | 4         | Too many nested blocks                   |

Any rule can be set to `"error"`, `"warning"`, or `"off"` in the config file.

---

## Configuration

Run `dotnet-arch init .` to generate a `dotnetarch.json` in your project root:

```jsonc
{
  // Layer keywords matched against namespace segments (case-insensitive)
  "layers": {
    "domain":         ["models", "entities", "dto", "exceptions"],
    "application":    ["services", "handlers"],
    "infrastructure": ["repositories", "context", "dataaccess"],
    "web":            ["controllers"]
  },

  // Rule severity: "error" | "warning" | "off"
  "rules": {
    "arch/layer-violation":       "warning",
    "arch/circular-dependency":   "error",
    "style/no-empty-catch":       "warning",
    "complexity/method-length":   "warning",
    "complexity/cyclomatic":      "off"
  },

  // Numeric thresholds — see table below for guidance
  "thresholds": {
    "complexity/method-length":   40,
    "complexity/cyclomatic":      10,
    "complexity/nesting-depth":   4
  }
}
```

### Threshold reference

|                              | Strict | Moderate | Lenient |
|------------------------------|--------|----------|---------|
| `arch/high-coupling`         | 5      | 10       | 15      |
| `complexity/method-length`   | 20     | 40       | 80      |
| `complexity/class-length`    | 150    | 300      | 500     |
| `complexity/parameter-count` | 3      | 5        | 8       |
| `complexity/cyclomatic`      | 5      | 10       | 20      |
| `complexity/nesting-depth`   | 2      | 4        | 6       |

**Strict** = Clean Code style (small, focused, easy to test)
**Moderate** = Pragmatic default for most codebases
**Lenient** = Useful when migrating a legacy codebase

You can set any integer value — this table is a guide only.

---

## CI/CD

`dotnet-arch` is designed to run in pipelines.

```yaml
# GitHub Actions
- name: Analyze architecture
  run: dotnet-arch analyze ./src --max-warnings 0
```

### Exit codes

| Code | Meaning                                        |
|------|------------------------------------------------|
| `0`  | No errors, warnings within threshold           |
| `1`  | One or more errors, or warnings exceed `--max-warnings` |

### `--max-warnings`

Fail the build if warnings exceed a limit:

```bash
dotnet-arch analyze . --max-warnings 10
```

### GitHub Actions annotations

When running inside GitHub Actions (`GITHUB_ACTIONS=true`), violations are automatically emitted as inline PR annotations:

```
::warning file=src/Services/UserService.cs,line=12::[style/no-empty-catch] Empty catch block...
```

---

## Output formats

```bash
# Default — colored terminal output
dotnet-arch analyze .

# JSON — useful for tooling and scripts
dotnet-arch analyze . --format json

# Save JSON to file
dotnet-arch analyze . --format json --output report.json
```

---

## Ignoring paths

Create a `.archignore` file in your project root:

```
# Auto-generated
Migrations/
Generated/

# Tests
Tests/
```

---

## How it works

1. Parses all `.cs` files using **Roslyn** (no build required)
2. Extracts types, methods, namespaces, and `using` directives
3. Maps namespace segments to architecture layers
4. Runs registered rules against the parsed model
5. Reports violations grouped by rule with file and line numbers

---

## Contributing

To add a new rule, implement `IArchRule`:

```csharp
public sealed class MyRule : IArchRule
{
    public string RuleId => "style/my-rule";

    public IEnumerable<AnalysisViolation> Analyze(
        IReadOnlyList<TypeInfo> types,
        IReadOnlyList<MethodInfo> methods,
        ArchConfig config)
    {
        if (!config.IsRuleEnabled(RuleId)) yield break;

        var severity = config.GetSeverityOverride(RuleId) ?? ViolationSeverity.Warning;

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
