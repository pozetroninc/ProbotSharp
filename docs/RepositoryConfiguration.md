# Repository Configuration

ProbotSharp provides repository-backed configuration with `context.config()`, matching the semantics of [Probot's configuration system](https://probot.github.io/docs/configuration/). This allows your GitHub App to load settings from configuration files in repositories, with support for cascading, inheritance, and flexible merge strategies.

## Table of Contents

- [Quick Start](#quick-start)
- [Configuration Resolution](#configuration-resolution)
- [Strongly-Typed Configuration](#strongly-typed-configuration)
- [Configuration Inheritance (_extends)](#configuration-inheritance-_extends)
- [Merge Strategies](#merge-strategies)
- [Advanced Options](#advanced-options)
- [Caching](#caching)
- [Examples](#examples)

## Quick Start

### Basic Usage

```text
public class MyBotHandler : IEventHandler
{
    public string EventName => "issues";
    public string? EventAction => "opened";

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        // Load configuration with defaults
        var config = await context.GetConfigAsync<MyBotSettings>(
            "mybot.yml",
            new MyBotSettings { WelcomeMessage = "Hello!" },
            cancellationToken);

        // Use configuration
        if (config != null)
        {
            context.Logger.LogInformation("Using message: {Message}", config.WelcomeMessage);
        }
    }
}

public class MyBotSettings
{
    public string WelcomeMessage { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}
```

### Configuration File

Create `.github/mybot.yml` in your repository:

```yaml
welcomeMessage: "👋 Thanks for opening this issue!"
enabled: true
```

## Configuration Resolution

ProbotSharp searches for configuration files in this order (cascading):

1. **Repository root** - `mybot.yml`
2. **Repository .github directory** - `.github/mybot.yml`
3. **Organization .github repository** - `{org}/.github/mybot.yml`

Later files override earlier ones, with deep merging of objects and configurable array merge strategies.

### Example Cascade

```
myorg/myrepo/mybot.yml          # Checked first (highest priority)
myorg/myrepo/.github/mybot.yml  # Checked second
myorg/.github/mybot.yml         # Checked last (fallback)
```

If no configuration file is found, the default values you provide are used.

## Strongly-Typed Configuration

### Define Your Settings Class

```text
public class BotSettings
{
    public string WelcomeMessage { get; set; } = "Welcome!";
    public List<string> Labels { get; set; } = new();
    public int MaxComments { get; set; } = 10;
    public ReviewSettings Review { get; set; } = new();
}

public class ReviewSettings
{
    public bool Enabled { get; set; } = false;
    public List<string> RequiredReviewers { get; set; } = new();
}
```

### Load Typed Configuration

```text
var config = await context.GetConfigAsync<BotSettings>(
    "bot-config.yml",
    new BotSettings(),
    cancellationToken);
```

### YAML File

```yaml
welcomeMessage: "👋 Welcome to our project!"
labels:
  - "needs-review"
  - "community"
maxComments: 5
review:
  enabled: true
  requiredReviewers:
    - "senior-dev"
    - "tech-lead"
```

## Configuration Inheritance (_extends)

Use `_extends` to inherit configuration from another repository:

### Parent Configuration

In `myorg/.github/bot-config.yml`:

```yaml
welcomeMessage: "Welcome to our organization!"
labels:
  - "community"
review:
  enabled: true
```

### Child Configuration

In `myorg/myrepo/.github/bot-config.yml`:

```yaml
_extends: myorg/.github
welcomeMessage: "Welcome to my specific repo!"
labels:
  - "community"
  - "myrepo-specific"
```

### Result

The child configuration extends the parent, with child values overriding parent values:

```yaml
welcomeMessage: "Welcome to my specific repo!"  # From child (overrides parent)
labels:                                         # Merged based on strategy
  - "community"
  - "myrepo-specific"
review:                                         # From parent (inherited)
  enabled: true
```

### _extends Formats

```yaml
# Full format with custom file
_extends: owner/repo:custom-config.yml

# Default file (config.yml)
_extends: owner/repo

# Same organization
_extends: other-repo

# Nested inheritance (parent can also extend)
_extends: myorg/base-config
```

### Circular Reference Protection

ProbotSharp automatically prevents circular references with a maximum depth limit (default: 5).

## Merge Strategies

Control how arrays are merged during configuration inheritance and cascading.

### ArrayMergeStrategy.Replace (Default)

Child arrays completely replace parent arrays:

```yaml
# Parent
items: [1, 2, 3]

# Child
items: [4, 5]

# Result
items: [4, 5]
```

### ArrayMergeStrategy.Concatenate

Child arrays are appended to parent arrays:

```yaml
# Parent
items: [1, 2, 3]

# Child
items: [4, 5]

# Result
items: [1, 2, 3, 4, 5]
```

### ArrayMergeStrategy.DeepMergeByIndex

Arrays are merged element-by-element by index:

```yaml
# Parent
reviewers:
  - name: alice
    role: lead
  - name: bob
    role: dev

# Child
reviewers:
  - role: senior-lead  # Merges with first item

# Result
reviewers:
  - name: alice        # Parent name preserved
    role: senior-lead  # Child role overrides
  - name: bob
    role: dev
```

### Setting Merge Strategy

```text
var options = new RepositoryConfigurationOptions
{
    ArrayMergeStrategy = ArrayMergeStrategy.Concatenate
};

var config = await context.GetConfigAsync(
    "config.yml",
    defaultConfig,
    options,
    cancellationToken);
```

## Advanced Options

### RepositoryConfigurationOptions

```text
var options = new RepositoryConfigurationOptions
{
    // Default configuration file name
    DefaultFileName = "mybot.yml",

    // Enable organization-wide configuration fallback
    EnableOrganizationConfig = true,

    // Enable .github directory cascade
    EnableGitHubDirectoryCascade = true,

    // Enable _extends inheritance
    EnableExtendsKey = true,

    // Cache TTL for configuration files
    CacheTtl = TimeSpan.FromMinutes(5),

    // Array merge strategy
    ArrayMergeStrategy = ArrayMergeStrategy.Concatenate,

    // Maximum _extends depth (circular reference protection)
    MaxExtendsDepth = 5
};
```

### Untyped Dictionary Configuration

For dynamic configuration structures:

```text
var config = await context.GetConfigAsync(
    "config.yml",
    new Dictionary<string, object> { ["enabled"] = true },
    cancellationToken);

if (config != null && config.TryGetValue("welcomeMessage", out var message))
{
    context.Logger.LogInformation("Message: {Message}", message);
}
```

## Caching

Configuration files are automatically cached for **5 minutes** to reduce GitHub API calls. The cache uses:

- **Cache key**: `{owner}/{repo}/{path}@{sha}`
- **Invalidation**: Automatic after TTL expires
- **InMemory**: In-memory cache (IMemoryCache)

### Cache Behavior

- First request fetches from GitHub API
- Subsequent requests within TTL use cached version
- SHA-based cache keys ensure correctness
- No manual cache management required

## Examples

### Complete Example: ConfigBot

See the full [ConfigBot example](../examples/ConfigBot) for a working implementation.

```text
public class ConfigBotHandler : IEventHandler
{
    public string EventName => "issues";
    public string? EventAction => "opened";

    public async Task HandleAsync(ProbotSharpContext context, CancellationToken cancellationToken = default)
    {
        var config = await context.GetConfigAsync<ConfigBotSettings>(
            "configbot.yml",
            new ConfigBotSettings(),
            cancellationToken);

        if (config == null || !config.EnableAutoLabel)
        {
            return;
        }

        var issue = context.Payload["issue"];
        var issueNumber = (int?)issue?["number"] ?? 0;
        var repo = context.Repository;

        if (repo != null)
        {
            // Post welcome comment
            await context.GitHub.Issue.Comment.Create(
                repo.Owner,
                repo.Name,
                issueNumber,
                config.WelcomeMessage);

            // Add labels
            await context.GitHub.Issue.Labels.AddToIssue(
                repo.Owner,
                repo.Name,
                issueNumber,
                config.DefaultLabels.ToArray());
        }
    }
}
```

### Example: Multi-Repository Setup

**Organization-wide defaults** (`myorg/.github/bot-config.yml`):

```yaml
welcomeMessage: "Welcome to {org}!"
labels:
  - "community"
review:
  enabled: false
```

**Specific repository** (`myorg/critical-repo/.github/bot-config.yml`):

```yaml
_extends: myorg/.github
review:
  enabled: true
  requiredReviewers:
    - "security-team"
labels:
  - "community"
  - "security-review"
```

### Example: Feature Flags

```text
public class FeatureFlags
{
    public bool EnableAutoMerge { get; set; } = false;
    public bool EnableNotifications { get; set; } = true;
    public Dictionary<string, bool> ExperimentalFeatures { get; set; } = new();
}

var flags = await context.GetConfigAsync<FeatureFlags>(
    "features.yml",
    new FeatureFlags(),
    cancellationToken);

if (flags?.EnableAutoMerge == true)
{
    // Auto-merge logic
}
```

## Best Practices

1. **Always Provide Defaults**: Ensure your app works even without configuration files

   ```text
   var config = await context.GetConfigAsync(
       "config.yml",
       new MySettings(), // Sensible defaults
       cancellationToken);
   ```

2. **Use Strongly-Typed Models**: Avoid runtime errors with compile-time type checking

3. **Document Your Configuration**: Include schema documentation in your bot's README

4. **Test Configuration Loading**: Write unit tests for configuration merging logic

5. **Handle Missing Configuration**: Log warnings when configuration is missing

   ```text
   if (config == null)
   {
       context.Logger.LogWarning("No configuration found, using defaults");
       config = new MySettings();
   }
   ```

6. **Version Your Configuration**: Consider adding a `version` field for migration support

## Troubleshooting

### Configuration Not Loading

- Verify the configuration file path and name
- Check repository permissions (app must have content read access)
- Review logs for parsing errors
- Ensure YAML syntax is valid

### Unexpected Merge Results

- Check the `ArrayMergeStrategy` setting
- Review the cascade order (root → .github → org)
- Inspect `_extends` chain for conflicts

### Cache Issues

- Configuration changes may take up to 5 minutes to reflect (TTL)
- SHA-based caching ensures correctness per file version
- Cache is per-process (not shared across app instances)

## API Reference

### Context Extension Methods

```text
// Strongly-typed
Task<T?> GetConfigAsync<T>(
    string? fileName = null,
    T? defaultConfig = null,
    CancellationToken cancellationToken = default) where T : class

// Untyped dictionary
Task<Dictionary<string, object>?> GetConfigAsync(
    string? fileName = null,
    Dictionary<string, object>? defaultConfig = null,
    CancellationToken cancellationToken = default)

// With custom options
Task<T?> GetConfigAsync<T>(
    string? fileName,
    T? defaultConfig,
    RepositoryConfigurationOptions options,
    CancellationToken cancellationToken = default) where T : class
```

### RepositoryConfigurationOptions

See [Advanced Options](#advanced-options) section for full API.

## See Also

- [ConfigBot Example](../examples/ConfigBot/README.md)
- [Probot Configuration Documentation](https://probot.github.io/docs/configuration/)
- [Architecture Documentation](./Architecture.md)
