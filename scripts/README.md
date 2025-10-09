# ProbotSharp Scripts

Utility scripts for maintaining and verifying the ProbotSharp repository.

## verify-github-links.py

Verifies that all GitHub URLs in Markdown files are accessible and return valid responses.

### Features

- ✅ Scans all Markdown files in the repository
- ✅ Extracts both markdown-style links and bare URLs
- ✅ Verifies each unique URL with HTTP requests
- ✅ Automatically skips placeholder URLs (your-org, yourusername, etc.)
- ✅ Provides detailed failure reports with file locations
- ✅ Supports rate limiting to avoid GitHub API throttling
- ✅ Colorized output for better readability
- ✅ Excludes specified directories (node_modules/, .aidocs/, etc.)

### Requirements

```bash
pip install requests
```

### Usage

**Basic usage:**
```bash
python3 scripts/verify-github-links.py
```

**Verbose output (show all links):**
```bash
python3 scripts/verify-github-links.py --verbose
```

**Custom exclusions:**
```bash
python3 scripts/verify-github-links.py --exclude vendor/ build/ tmp/
```

**Adjust timeouts and rate limiting:**
```bash
# 20 second timeout, 1 second delay between requests
python3 scripts/verify-github-links.py --timeout 20 --delay 1.0
```

**Include placeholder URLs in verification:**
```bash
# Don't skip placeholder URLs (your-org, yourusername, etc.)
python3 scripts/verify-github-links.py --no-skip-placeholders
```

**Show help:**
```bash
python3 scripts/verify-github-links.py --help
```

### Output

The script provides:

1. **Scan summary**: Number of Markdown files found
2. **Link extraction**: Total links and unique URLs
3. **Verification progress**: Real-time verification status
4. **Summary statistics**: Success/failure counts
5. **Detailed failure report**: Failed URLs with file locations

**Example output:**
```
GitHub Link Verification Tool
Repository: /home/user/probot-sharp
Excluded patterns: node_modules/, .git/, .aidocs/

Scanning for Markdown files...
Found 69 Markdown files

Extracting GitHub links...
Found 73 GitHub link(s) (29 unique URLs)

Verifying links...
Skipping 6 placeholder URL(s)

======================================================================
Summary
======================================================================
Total unique URLs: 29
Total occurrences: 73
Skipped (placeholders): 6
Verified: 23
Failed: 0

✓ All GitHub links verified successfully!
```

**With verbose mode:**
```
Skipped Placeholder URLs:

⊘ https://github.com/your-org/probot-sharp.git
  Found in 5 location(s)
⊘ https://github.com/yourusername/probotsharp.git
  Found in 3 location(s)
```

### Exit Codes

- `0` - All links verified successfully
- `1` - One or more links failed verification

### CI/CD Integration

The script is integrated into both pre-commit hooks and CI/CD pipelines:

**✅ Pre-commit Hook (Husky.Net):**

Configured in `.husky/task-runner.json` and runs automatically on every commit:
```bash
dotnet husky run --group pre-commit
```

**✅ GitHub Actions:**

Configured in `.github/workflows/dotnet.yml` as a separate job:
```yaml
verify-links:
  name: Verify GitHub Links
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - name: Setup Python
      uses: actions/setup-python@v5
      with:
        python-version: '3.x'
    - name: Install dependencies
      run: pip install requests
    - name: Verify GitHub links in Markdown files
      run: python3 scripts/verify-github-links.py
```

**Manual Testing:**
```bash
# Test locally before committing
python3 scripts/verify-github-links.py

# Or run pre-commit hook manually
dotnet husky run --group pre-commit
```

### Placeholder URL Handling

By default, the script skips URLs containing common placeholder patterns:
- `your-org`
- `your-repo`
- `your-bot`
- `yourusername`
- `yourname`
- `example.com`
- `example-org`

These placeholders are commonly used in documentation examples. Skipped URLs:
- Don't count as failures (exit code remains 0)
- Are reported in the summary count
- Can be viewed with `--verbose` flag
- Can be verified with `--no-skip-placeholders` flag

### Known Limitations

- **Rate Limiting**: GitHub may rate-limit requests. Use `--delay` to slow down.
- **Private Repos**: Links to private repositories will fail unless authenticated.
- **Branch Names**: Links with specific branches (not `master` or `main`) may fail if branch doesn't exist.

### Troubleshooting

**requests module not found:**
```bash
pip install requests
# or
pip3 install requests
```

**Too many 403 errors:**
```bash
# Increase delay between requests
python3 scripts/verify-github-links.py --delay 2.0
```

**Timeout errors:**
```bash
# Increase timeout
python3 scripts/verify-github-links.py --timeout 30
```

## verify-local-links.py

Verifies local links in Markdown files, including file references, anchors, and cross-document links.

### Features

- ✅ Validates local file references (relative and repository-root paths)
- ✅ Validates intra-document anchors (headings in same file)
- ✅ Validates cross-document anchors (headings in other files)
- ✅ Validates image file references
- ✅ GitHub-compatible anchor slugification
- ✅ Skips external HTTP/HTTPS links (handled by verify-github-links.py)
- ✅ Skips source code line number references (e.g., `file.cs#L123`)
- ✅ Handles repository-root relative paths (e.g., `/docs/file.md`)
- ✅ Colorized output for better readability
- ✅ Excludes specified directories

### Requirements

```bash
# No additional dependencies - uses Python standard library
python3 --version  # Python 3.6+
```

### Usage

**Basic usage:**
```bash
python3 scripts/verify-local-links.py
```

**Verbose output (show all links including successful):**
```bash
python3 scripts/verify-local-links.py --verbose
```

**Custom exclusions:**
```bash
python3 scripts/verify-local-links.py --exclude vendor/ build/ tmp/
```

### Link Types Validated

| Type | Example | Validation |
|------|---------|------------|
| **Local file** | `[docs](../docs/Architecture.md)` | File exists |
| **Intra-document anchor** | `[see below](#features)` | Header exists in current file |
| **Cross-document anchor** | `[config](../docs/Architecture.md#port-interfaces)` | File exists + header exists |
| **Image reference** | Image links (with `!` prefix) | Image file exists |
| **Repository-root path** | `[docs](/docs/Architecture.md)` | File exists relative to repo root |
| **Reference-style** | Example: `[link][ref]` with `[ref]: path` | Resolved reference target exists |

### Link Types Skipped

- ✅ **External links** - HTTP/HTTPS URLs (handled by verify-github-links.py)
- ✅ **Source code line refs** - Non-markdown files with anchors (e.g., `Program.cs#L45`)
- ✅ **Mail links** - `mailto:` URLs
- ✅ **FTP links** - `ftp://` URLs

### Anchor Slugification

The script uses GitHub-compatible anchor slugification rules:

```python
# GitHub's rules:
"Testing & Coverage"  →  "testing--coverage"
"Docker Compose (v2)" →  "docker-compose-v2"
"API: Core Methods"   →  "api-core-methods"
```

**Rules applied:**
1. Convert to lowercase
2. Replace spaces with hyphens
3. Remove special characters (keep alphanumeric, hyphens, underscores)
4. Keep multiple consecutive hyphens (from removed characters)
5. Handle duplicate headers by appending `-1`, `-2`, etc.

### Output

**Example output:**
```
Local Link Verification Tool
Repository: /home/user/probot-sharp
Excluded patterns: node_modules/, .git/, .aidocs/

Scanning for Markdown files...
Found 58 Markdown files

Building anchor map for all files...
Built anchor map for 58 files

Validating local links...

======================================================================
Summary
======================================================================
Valid links: 362
Broken links: 23

Broken Links:

✗ README.md:17
  Link: #docker-compose-recommended
  Error: Anchor '#docker-compose-recommended' not found in document

✗ docs/Architecture.md:142
  Link: ./missing.md
  Error: File not found: ./missing.md

✗ docs/Guide.md:56
  Link: Architecture.md#nonexistent-section
  Error: Anchor '#nonexistent-section' not found in Architecture.md
```

**With verbose mode:**
```
✓ README.md:12 - ./docs/Architecture.md
✓ README.md:15 - #features
✗ README.md:17 - #docker-compose-recommended
  Anchor '#docker-compose-recommended' not found in document
⊘ Skipping non-markdown anchor: Program.cs#L45
```

### Exit Codes

- `0` - All local links verified successfully
- `1` - One or more links failed verification

### CI/CD Integration

**Husky.Net (Pre-commit Hook):**

Add to `.husky/task-runner.json`:
```json
{
  "tasks": [
    {
      "name": "verify-local-links",
      "command": "bash",
      "args": ["-c", "python3 scripts/verify-local-links.py"]
    }
  ]
}
```

**GitHub Actions:**

```yaml
verify-local-links:
  name: Verify Local Links
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - name: Setup Python
      uses: actions/setup-python@v5
      with:
        python-version: '3.x'
    - name: Verify local links in Markdown files
      run: python3 scripts/verify-local-links.py
```

**Combined with verify-github-links.py:**

```yaml
verify-links:
  name: Verify All Links
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - name: Setup Python
      uses: actions/setup-python@v5
      with:
        python-version: '3.x'
    - name: Install dependencies
      run: pip install requests
    - name: Verify local links
      run: python3 scripts/verify-local-links.py
    - name: Verify external GitHub links
      run: python3 scripts/verify-github-links.py
```

### Common Issues

**False positives for anchors:**
- Ensure header text matches exactly (case-insensitive after slugification)
- Check for special characters that get removed
- Verify duplicate headers aren't causing `-1` suffix mismatches

**Repository-root paths not resolving:**
- Paths starting with `/` are treated as repository-root relative
- Example: `/docs/file.md` resolves to `<repo>/docs/file.md`

**Source code references flagged:**
- Links like `file.cs#L123` are automatically skipped
- Only markdown files have anchor validation

### Comparison with verify-github-links.py

| Feature | verify-local-links.py | verify-github-links.py |
|---------|----------------------|------------------------|
| Local file refs | ✅ Yes | ❌ No |
| Anchors | ✅ Yes | ❌ No |
| Images | ✅ Yes | ❌ No |
| External HTTP links | ❌ Skipped | ✅ Yes |
| Network required | ❌ No | ✅ Yes |
| Speed | ⚡ Fast | 🐢 Slower (rate limiting) |

**Recommendation**: Run both tools in your CI/CD pipeline for comprehensive link validation.

### Troubleshooting

**Anchor mismatches:**
```bash
# Use verbose mode to see what anchors were found
python3 scripts/verify-local-links.py --verbose | grep "✓"
```

**Check slugification:**
```python
# Test how a header is slugified
python3 -c "
import re
text = 'Your Header & Title (v2)'
slug = text.lower().replace(' ', '-')
slug = re.sub(r'[^\w\-]', '', slug, flags=re.UNICODE)
print(slug)
"
```

**Many broken links:**
```bash
# Save report to file for analysis
python3 scripts/verify-local-links.py > link-report.txt 2>&1
```

## verify-markdown-code.sh / verify-markdown-code.ps1

Verifies that C# code blocks in Markdown documentation are syntactically correct and compile successfully.

### Features

- ✅ Scans all Markdown files for C# code blocks
- ✅ Compiles code blocks against project assemblies
- ✅ Detects syntax errors, missing usings, and type errors
- ✅ Provides detailed error messages with file locations
- ✅ Supports custom exclusion patterns
- ✅ Cross-platform (Bash for Linux/Mac, PowerShell for Windows)

### Requirements

- .NET SDK 8.0+
- Repository must be built first (generates assemblies for reference)

### Usage

**Linux/Mac (Bash):**
```bash
# Basic usage - verifies all Markdown files
bash scripts/verify-markdown-code.sh

# With additional arguments (passed to MarkdownCodeVerifier)
bash scripts/verify-markdown-code.sh --verbose
```

**Windows (PowerShell):**
```powershell
# Basic usage
pwsh scripts/verify-markdown-code.ps1

# With additional arguments
pwsh scripts/verify-markdown-code.ps1 --verbose
```

### How It Works

1. Builds the project in Release configuration to generate assemblies
2. Runs the `tools/MarkdownCodeVerifier` tool against the repository
3. Excludes common directories (`node_modules/`, `.git/`, `.aidocs/`) and `TEST-MARKDOWN-VERIFIER.md` by default
4. Exits with code 0 if all code blocks compile, 1 if errors found

### Default Exclusions

- `node_modules/` - Third-party dependencies
- `.git/` - Git repository metadata
- `.aidocs/` - AI documentation artifacts
- `TEST-MARKDOWN-VERIFIER.md` - Test file for the verifier itself

### Example Output

```
Markdown C# Code Verifier

Building project for assembly references...
✓ Build complete

Verifying C# code blocks in Markdown files...
✓ docs/EventHandlers.md (3 code blocks)
✓ docs/Extensions.md (5 code blocks)
✗ docs/Example.md (2 code blocks)
  - Line 45: CS0246: The type or namespace name 'InvalidType' could not be found

Summary: 10 files checked, 8 passed, 2 failed
```

### CI/CD Integration

**Husky.Net (Pre-commit Hook):**

Configured in `.husky/task-runner.json`:
```bash
dotnet husky run --group pre-commit
```

**GitHub Actions:**

```yaml
- name: Verify Markdown code blocks
  run: bash scripts/verify-markdown-code.sh
```

**Make Integration:**

```bash
make verify-markdown
```

## run-kubeconform.sh / run-kubeconform.ps1

Validates Kubernetes YAML manifests using kubeconform with offline schema validation.

### Features

- ✅ Validates Kubernetes YAML syntax and structure
- ✅ Auto-downloads kubeconform if not installed (v0.6.7)
- ✅ Uses offline schema cache for faster validation
- ✅ Validates only staged files in pre-commit hook context
- ✅ Strict validation mode enabled
- ✅ Supports both Linux/Mac (Bash) and Windows (PowerShell)
- ✅ Excludes Helm templates automatically

### Requirements

**Linux/Mac:**
- git, curl, tar (for auto-download)

**Windows:**
- PowerShell 5.1+ or PowerShell Core 7+
- git

**Schema Cache:**
- Requires `tools/kubeconform/schemas/` directory with Kubernetes schemas

### Usage

**Linux/Mac (Bash):**
```bash
# Run as pre-commit hook (validates staged files)
bash scripts/run-kubeconform.sh

# Set custom kubeconform binary location
KUBECONFORM_BIN=/usr/local/bin/kubeconform bash scripts/run-kubeconform.sh
```

**Windows (PowerShell):**
```powershell
# Run as pre-commit hook
pwsh scripts/run-kubeconform.ps1

# Set custom kubeconform binary
$env:KUBECONFORM_BIN = "C:\tools\kubeconform.exe"
pwsh scripts/run-kubeconform.ps1
```

### Validated Files

The script validates files that match **all** of these criteria:
- ✅ Staged for commit (git diff --cached)
- ✅ YAML extension (`.yml` or `.yaml`)
- ✅ Located in `deploy/k8s/` or `deploy/kubernetes/`
- ❌ NOT in `deploy/kubernetes/helm/` (Helm templates excluded)

### Auto-Download Behavior

If kubeconform is not found in:
1. `$KUBECONFORM_BIN` environment variable
2. System PATH
3. `tools/kubeconform/` directory

The script will automatically:
1. Detect OS and architecture
2. Download appropriate binary from GitHub releases
3. Extract to `tools/kubeconform/`
4. Use the downloaded binary

**Supported platforms:**
- Linux: amd64, arm64
- macOS (Darwin): amd64, arm64
- Windows: amd64, arm64

### Schema Cache

The script requires a local schema cache at `tools/kubeconform/schemas/`. The schema template used is:

```
tools/kubeconform/schemas/{{ .NormalizedKubernetesVersion }}-standalone{{ .StrictSuffix }}/{{ .ResourceKind }}{{ .KindSuffix }}.json
```

This enables offline validation without network requests.

### Example Output

```
kubeconform: validating 3 file(s)
deploy/kubernetes/deployment.yaml - Deployment example-app is valid
deploy/kubernetes/service.yaml - Service example-app is valid
deploy/kubernetes/ingress.yaml - Ingress example-app is valid
```

**With errors:**
```
kubeconform: validating 1 file(s)
deploy/kubernetes/deployment.yaml - Deployment example-app is invalid:
  spec.template.spec.containers.0.image: Required value
```

### CI/CD Integration

**Husky.Net (Pre-commit Hook):**

Configured in `.husky/task-runner.json` to run on Kubernetes YAML changes:
```bash
dotnet husky run --group pre-commit
```

**GitHub Actions:**

```yaml
- name: Validate Kubernetes manifests
  run: bash scripts/run-kubeconform.sh
```

### Troubleshooting

**Schema cache not found:**
```
kubeconform: expected schema cache at 'tools/kubeconform/schemas' not found
```
Ensure you have the schema cache directory committed to the repository.

**Download fails:**
```bash
# Install kubeconform manually
curl -L https://github.com/yannh/kubeconform/releases/download/v0.6.7/kubeconform-linux-amd64.tar.gz | tar xz
sudo mv kubeconform /usr/local/bin/
```

**Unsupported OS/architecture:**
```
kubeconform: unsupported OS 'xyz'. Please install kubeconform manually.
```
Download the appropriate binary from [kubeconform releases](https://github.com/yannh/kubeconform/releases) and set `KUBECONFORM_BIN`.

## Other Scripts

See subdirectories for additional scripts:
- `testing/` - Testing infrastructure for examples and bots (see [testing/README.md](testing/README.md))
