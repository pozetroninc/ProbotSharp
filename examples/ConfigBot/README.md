# ConfigBot Example

This example demonstrates how to use repository-backed configuration with ProbotSharp's `context.config()` API.

## Features

- **Repository Configuration**: Loads settings from `.github/configbot.yml` in the repository
- **Cascading Support**: Falls back to organization-wide `.github` repository if not found in repo
- **Default Values**: Provides sensible defaults if no configuration file exists
- **Strongly-Typed**: Uses C# classes for type-safe configuration access

## Configuration File

Create `.github/configbot.yml` in your repository:

```yaml
welcomeMessage: "👋 Thanks for opening this issue! We'll get back to you soon."
enableAutoLabel: true
defaultLabels:
  - "needs-triage"
  - "community"
maxCommentsPerIssue: 10
```

## Configuration Cascade

ConfigBot searches for configuration in this order:

1. `configbot.yml` in repository root
2. `.github/configbot.yml` in repository
3. `.github/configbot.yml` in organization's `.github` repository
4. Default values from code

## Running the Example

1. Set up your GitHub App credentials:
   ```bash
   export GITHUB_APP_ID=your_app_id
   export GITHUB_PRIVATE_KEY_PATH=/path/to/private-key.pem
   export GITHUB_WEBHOOK_SECRET=your_webhook_secret
   ```

2. Run the bot:
   ```bash
   cd examples/ConfigBot
   dotnet run
   ```

3. Install your GitHub App on a test repository

4. Create an issue - ConfigBot will:
   - Load the configuration from `.github/configbot.yml` (or use defaults)
   - Post a welcome comment using the configured message
   - Add labels if `enableAutoLabel` is true

## Advanced: Using _extends

You can extend configurations from other repositories:

```yaml
# In myorg/.github repo's configbot.yml (base config)
welcomeMessage: "Thanks for contributing to our organization!"
enableAutoLabel: true
defaultLabels:
  - "community"

# In myorg/myrepo repo's configbot.yml (override)
_extends: myorg/.github
welcomeMessage: "Thanks for contributing to myrepo specifically!"
defaultLabels:
  - "community"
  - "myrepo-specific"
```

The configuration system will:
1. Load the base config from `myorg/.github`
2. Merge it with the repo-specific config
3. Child values override parent values

## API Examples

### Basic Usage

```text
var config = await context.GetConfigAsync<MySettings>(
    "mybot.yml",
    new MySettings { DefaultValue = "fallback" },
    cancellationToken);
```

### Untyped Dictionary

```text
var config = await context.GetConfigAsync(
    "config.yml",
    new Dictionary<string, object> { ["key"] = "default" },
    cancellationToken);
```

### Custom Options

```text
var options = new RepositoryConfigurationOptions
{
    EnableOrganizationConfig = false,
    ArrayMergeStrategy = ArrayMergeStrategy.Concatenate,
    MaxExtendsDepth = 3
};

var config = await context.GetConfigAsync(
    "mybot.yml",
    defaultConfig,
    options,
    cancellationToken);
```

## Testing Configuration

You can test configuration loading without a live GitHub App:

1. Create a test repository with `.github/configbot.yml`
2. Use Smee.io to proxy webhooks to localhost
3. Open an issue and watch the logs

The handler will log the loaded configuration values for debugging.
