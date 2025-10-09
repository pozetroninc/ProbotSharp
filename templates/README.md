# Probot Sharp Templates

This directory contains `dotnet new` templates for creating Probot Sharp applications.

## Available Templates

### probotsharp-app

Creates a new Probot Sharp GitHub App with:
- Sample event handler for `issues.opened`
- Complete project structure
- Environment configuration template
- README with getting started guide

## Local Installation (Development)

To install the templates locally for development:

```bash
# From the templates directory
dotnet new install .
```

## Usage

Create a new bot:

```bash
# Basic usage (creates MyBot)
dotnet new probotsharp-app

# Specify custom name
dotnet new probotsharp-app -n MyAwesomeBot

# Specify name and output directory
dotnet new probotsharp-app -n MyAwesomeBot -o ./my-bots/awesome-bot

# With all parameters
dotnet new probotsharp-app \
  -n MyAwesomeBot \
  -o ./my-bot \
  --AppName "MyAwesomeBot" \
  --Description "My awesome GitHub bot" \
  --Author "John Doe"
```

## Template Parameters

- `--AppName` or `-n` - Name of the bot (default: "MyBot")
- `--Description` - Description of what the bot does (default: "A Probot Sharp bot")
- `--Author` - Your name (default: "Your Name")
- `--AppId` - GitHub App ID for .env.example (default: "123456")

## Uninstalling

To uninstall the templates:

```bash
dotnet new uninstall ProbotSharp.Templates
```

## Publishing (NuGet Package)

To build the NuGet package:

```bash
# From the templates directory
dotnet pack ProbotSharp.Templates.csproj -o ./nupkg

# Install from local package
dotnet new install ./nupkg/ProbotSharp.Templates.1.0.0.nupkg

# Publish to NuGet.org
dotnet nuget push ./nupkg/ProbotSharp.Templates.1.0.0.nupkg --source https://api.nuget.org/v3/index.json --api-key YOUR_API_KEY
```

Once published, users can install with:

```bash
dotnet new install ProbotSharp.Templates
```

## Testing Templates

1. **Install locally**:
   ```bash
   dotnet new install .
   ```

2. **Generate a test project**:
   ```bash
   dotnet new probotsharp-app -n TestBot -o /tmp/TestBot
   ```

3. **Build and verify**:
   ```bash
   cd /tmp/TestBot
   dotnet build
   ```

4. **Clean up**:
   ```bash
   rm -rf /tmp/TestBot
   dotnet new uninstall ProbotSharp.Templates
   ```

## Template Structure

```
templates/
├── ProbotSharp.Templates.csproj    # NuGet package definition
├── README.md                        # This file
└── probotsharp-app/                # Template content
    ├── .template.config/
    │   └── template.json           # Template manifest
    ├── Handlers/
    │   └── ExampleHandler.cs       # Sample event handler
    ├── .env.example                # Environment variables
    ├── .gitignore                  # Git ignore file
    ├── MyBot.csproj                # Project file
    ├── MyBotApp.cs                 # Main app class
    ├── Program.cs                  # Entry point
    └── README.md                   # Generated project README
```

## Customization

To customize the templates:

1. Edit files in `probotsharp-app/`
2. Update parameter substitutions in `.template.config/template.json`
3. Increment version in `ProbotSharp.Templates.csproj`
4. Reinstall: `dotnet new uninstall ProbotSharp.Templates && dotnet new install .`

## Troubleshooting

**Template not found after installation:**
```bash
# List installed templates
dotnet new list

# Check for ProbotSharp.Templates
dotnet new list | grep -i probot
```

**Template installation fails:**
```bash
# Clear template cache
rm -rf ~/.templateengine

# Try installing again
dotnet new install .
```

**Generated project doesn't build:**
- Check that project references point to correct paths
- Verify all ProbotSharp packages are built
- Check .NET 8.0 SDK is installed: `dotnet --version`

## Resources

- [Custom templates for dotnet new](https://docs.microsoft.com/en-us/dotnet/core/tools/custom-templates)
- [Template JSON schema](http://json.schemastore.org/template)
- [Creating NuGet packages](https://docs.microsoft.com/en-us/nuget/create-packages/creating-a-package)
