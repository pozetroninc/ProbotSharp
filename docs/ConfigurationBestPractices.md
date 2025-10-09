# Configuration Best Practices

This guide provides best practices for configuring ProbotSharp applications for development, testing, and production environments.

## Table of Contents

- [Configuration File Structure](#configuration-file-structure)
- [JSON Schema for IntelliSense](#json-schema-for-intellisense)
- [Inline Documentation](#inline-documentation)
- [Explicit Options](#explicit-options)
- [Environment-Specific Configuration](#environment-specific-configuration)
- [Secrets Management](#secrets-management)
- [Complete Example](#complete-example)

## Configuration File Structure

ProbotSharp uses `appsettings.json` for configuration. A well-structured configuration file should be:

1. **Self-documenting** - Include JSON schema and inline comments
2. **Explicit** - Show all available options, even with default values
3. **Validated** - Use placeholders that make missing values obvious
4. **Environment-aware** - Support different configs for dev/staging/prod

## JSON Schema for IntelliSense

Always include the JSON schema reference at the top of your configuration file to enable IntelliSense and validation in VS Code and other editors:

```json
{
  "$schema": "https://json.schemastore.org/appsettings.json",

  "ProbotSharp": {
    // ... configuration
  }
}
```

**Benefits:**
- **IntelliSense** - Auto-completion for ASP.NET Core settings
- **Validation** - Real-time error detection for malformed JSON
- **Documentation** - Inline hints for standard .NET configuration keys

## Inline Documentation

Use `_comment` keys to document complex configuration sections:

```json
{
  "ProbotSharp": {
    "_comment": "GitHub App Configuration - See: https://docs.github.com/apps",

    "AppId": "YOUR_GITHUB_APP_ID",
    "WebhookSecret": "development",
    "PrivateKeyPath": "private-key.pem",

    "Adapters": {
      "_comment": "Adapter Configuration - Explicit Provider Selection. See: docs/AdapterConfiguration.md",

      "Cache": {
        "_comment": "Memory: No external dependencies. Redis: Shared cache across instances.",
        "Provider": "InMemory",
        "Options": {
          "ExpirationMinutes": "60"
        }
      }
    }
  }
}
```

**Why this matters:**
- Helps new developers understand configuration choices
- Documents trade-offs between different adapter providers
- References relevant documentation for deeper details

## Explicit Options

Always include `Options` objects with explicit default values, even if they match framework defaults:

**❌ Bad - Implicit defaults:**
```json
{
  "Cache": {
    "Provider": "InMemory"
  }
}
```

**✅ Good - Explicit options:**
```json
{
  "Cache": {
    "Provider": "InMemory",
    "Options": {
      "ExpirationMinutes": "60"
    }
  }
}
```

**Benefits:**
- **Discoverability** - Developers see what options are available
- **Clarity** - No guessing about what defaults are being used
- **Intentionality** - Explicit values show deliberate configuration choices

## Environment-Specific Configuration

Use `appsettings.{Environment}.json` to override settings per environment:

**appsettings.json** (Base configuration):
```json
{
  "ProbotSharp": {
    "Adapters": {
      "Cache": {
        "Provider": "InMemory"
      },
      "Idempotency": {
        "Provider": "Database"
      },
      "Persistence": {
        "Provider": "InMemory"
      }
    }
  }
}
```

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "ProbotSharp": "Debug"
    }
  },
  "ProbotSharp": {
    "WebhookSecret": "development"
  }
}
```

**appsettings.Production.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "ProbotSharp": "Warning"
    }
  },
  "ProbotSharp": {
    "Adapters": {
      "Cache": {
        "Provider": "Redis",
        "Options": {
          "ConnectionString": "REDIS_CONNECTION_STRING_FROM_ENV",
          "ExpirationMinutes": "120"
        }
      },
      "Idempotency": {
        "Provider": "Redis",
        "Options": {
          "ConnectionString": "REDIS_CONNECTION_STRING_FROM_ENV",
          "ExpirationHours": "48"
        }
      },
      "Persistence": {
        "Provider": "PostgreSQL",
        "Options": {
          "ConnectionString": "DATABASE_CONNECTION_STRING_FROM_ENV"
        }
      }
    }
  }
}
```

## Secrets Management

**Never commit secrets to source control.** Use environment variables and secure secret stores:

### Development

Use User Secrets for local development:

```bash
# Initialize user secrets
dotnet user-secrets init

# Set secrets
dotnet user-secrets set "ProbotSharp:WebhookSecret" "my-dev-secret"
dotnet user-secrets set "ProbotSharp:PrivateKey" "$(cat private-key.pem)"
```

In `appsettings.json`, use placeholder values:

```json
{
  "ProbotSharp": {
    "AppId": "YOUR_GITHUB_APP_ID",
    "WebhookSecret": "YOUR_WEBHOOK_SECRET",
    "PrivateKeyPath": "private-key.pem"
  }
}
```

### Production

Use environment variables or cloud secret managers:

**Environment Variables:**
```bash
export ProbotSharp__AppId="123456"
export ProbotSharp__WebhookSecret="prod-secret-from-keyvault"
export ProbotSharp__Adapters__Cache__Options__ConnectionString="redis-prod.azure.com:6380,ssl=true"
```

**Azure Key Vault:**
```text
// Program.cs - Requires: Azure.Identity, Azure.Extensions.AspNetCore.Configuration.Secrets packages
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

**AWS Secrets Manager:**
```text
// Program.cs - Requires: AWSSDK.Extensions.NETCore.Setup package
builder.Configuration.AddSecretsManager(
    region: RegionEndpoint.USEast1,
    configurator: options =>
    {
        options.SecretFilter = entry => entry.Name.StartsWith("ProbotSharp/");
    });
```

## Complete Example

Here's a production-ready `appsettings.json` following all best practices:

```json
{
  "$schema": "https://json.schemastore.org/appsettings.json",

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ProbotSharp": "Debug"
    }
  },
  "AllowedHosts": "*",

  "ProbotSharp": {
    "_comment": "GitHub App Configuration - Get these values from https://github.com/settings/apps",

    "AppId": "YOUR_GITHUB_APP_ID",
    "WebhookSecret": "YOUR_WEBHOOK_SECRET",
    "PrivateKeyPath": "private-key.pem",

    "_comment_adapters": "Adapter Configuration - Explicit Provider Selection. See: docs/AdapterConfiguration.md for complete guide",

    "Adapters": {
      "Cache": {
        "_comment": "Memory: No external dependencies. Redis: Shared cache across instances.",
        "Provider": "InMemory",
        "Options": {
          "ExpirationMinutes": "60",
          "_comment": "For Redis, add: ConnectionString, InstanceName"
        }
      },

      "Idempotency": {
        "_comment": "Database: Uses in-memory SQLite. Redis: Distributed idempotency. Both prevent duplicate webhook processing.",
        "Provider": "Database",
        "Options": {
          "ExpirationHours": "24",
          "_comment": "For Redis, add: ConnectionString, InstanceName. For Database with PostgreSQL, add: ConnectionString"
        }
      },

      "Persistence": {
        "_comment": "InMemory: Lost on restart. PostgreSQL: Persistent storage for webhooks and metadata.",
        "Provider": "InMemory",
        "Options": {
          "_comment": "For PostgreSQL, add: ConnectionString, MaxRetryCount, MaxRetryDelay"
        }
      },

      "ReplayQueue": {
        "_comment": "InMemory: Single-instance queue. Redis: Shared queue for failed webhook replay.",
        "Provider": "InMemory",
        "Options": {
          "MaxRetryAttempts": "3",
          "RetryBaseDelayMs": "1000",
          "PollIntervalMs": "1000",
          "_comment": "For Redis, add: ConnectionString, InstanceName"
        }
      },

      "DeadLetterQueue": {
        "_comment": "InMemory: Lost on restart. Redis/Database: Persistent storage for failed webhooks.",
        "Provider": "InMemory",
        "Options": {
          "_comment": "For Redis, add: ConnectionString, InstanceName"
        }
      },

      "Metrics": {
        "_comment": "NoOp: No metrics. OpenTelemetry: Export to Prometheus/Grafana/etc.",
        "Provider": "NoOp",
        "Options": {
          "_comment": "For OpenTelemetry, configure exporters via standard OpenTelemetry configuration"
        }
      },

      "Tracing": {
        "_comment": "NoOp: No tracing. OpenTelemetry: Distributed tracing with Jaeger/Zipkin/etc.",
        "Provider": "NoOp",
        "Options": {
          "_comment": "For OpenTelemetry, configure exporters via standard OpenTelemetry configuration"
        }
      }
    }
  }
}
```

## Best Practices Checklist

When creating or reviewing `appsettings.json`:

- [ ] JSON schema reference included (`$schema`)
- [ ] Inline comments explain adapter choices (`_comment` keys)
- [ ] All adapter options explicitly documented
- [ ] Placeholder values clearly marked (e.g., `YOUR_GITHUB_APP_ID`)
- [ ] No secrets committed (use placeholders instead)
- [ ] Environment-specific overrides in `appsettings.{Environment}.json`
- [ ] Production config uses external stores (Redis, PostgreSQL)
- [ ] Development config uses in-memory providers for simplicity
- [ ] Comments reference relevant documentation sections

## See Also

- [AdapterConfiguration.md](./AdapterConfiguration.md) - Complete adapter configuration guide
- [LocalDevelopment.md](./LocalDevelopment.md) - Development environment setup
- [Deployment.md](./Deployment.md) - Production deployment guides
- [Security Best Practices](./BestPractices.md#security) - Secrets management patterns
