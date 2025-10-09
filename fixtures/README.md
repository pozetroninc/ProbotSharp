# Webhook Fixtures

This directory contains example GitHub webhook payloads for testing and local development.

## Available Fixtures

- **issues-opened.json** - Example payload for when an issue is opened
- **pull-request-opened.json** - Example payload for when a pull request is opened
- **push.json** - Example payload for when code is pushed to a repository
- **test-webhook-payload.json** - Minimal test payload for webhook testing scripts

## Usage

These fixtures can be used with the `receive` command to simulate webhook events locally:

```bash
# Replay an issue opened event
dotnet run --project src/ProbotSharp.Bootstrap.Cli -- receive ./your-app -e issues.opened -f fixtures/issues-opened.json

# Replay a pull request opened event
dotnet run --project src/ProbotSharp.Bootstrap.Cli -- receive ./your-app -e pull_request.opened -f fixtures/pull-request-opened.json

# Replay a push event
dotnet run --project src/ProbotSharp.Bootstrap.Cli -- receive ./your-app -e push -f fixtures/push.json
```

## Creating Your Own Fixtures

To create custom webhook fixtures:

1. **From GitHub webhook delivery page:**
   - Go to your GitHub App settings
   - Navigate to "Advanced" → "Recent Deliveries"
   - Click on a delivery to view the payload
   - Copy the JSON payload

2. **From application logs:**
   - Trigger a webhook event in GitHub
   - Check your application logs for the received payload
   - Copy the payload JSON

3. **From GitHub documentation:**
   - Visit [GitHub Webhook Events Documentation](https://docs.github.com/en/webhooks/webhook-events-and-payloads)
   - Find the event type you need
   - Copy the example payload

4. Save the payload as a `.json` file in this directory

## Fixture Format

All fixtures should be valid GitHub webhook payloads and include:

- **action** - The event action (e.g., "opened", "closed")
- **repository** - Repository information including owner and name
- **installation** - GitHub App installation details
- **sender** - User who triggered the event
- Event-specific data (e.g., `issue`, `pull_request`, `commits`)

## Testing with Real GitHub API

If you want to test API calls to GitHub while replaying fixtures:

```bash
dotnet run --project src/ProbotSharp.Bootstrap.Cli -- receive ./your-app \
  -e issues.opened \
  -f fixtures/issues-opened.json \
  --app-id YOUR_APP_ID \
  --private-key path/to/private-key.pem
```

This will authenticate your app with GitHub and allow you to make real API calls during testing.

## Testing with Docker

The **test-webhook-payload.json** fixture is used by the testing scripts in `scripts/testing/`:

```bash
# Send webhook to running bot
./scripts/testing/send-test-webhook.sh http://localhost:5000/webhooks

# Test example with Docker
./scripts/testing/test-example.sh MinimalBot

# Test all examples
./scripts/testing/test-all-examples.sh
```

See [scripts/testing/README.md](../scripts/testing/README.md) for details.

## Notes

- The `installation.id` values in these fixtures are fake and won't work with real GitHub API calls
- Repository and user IDs are also fake and for testing only
- To test with real GitHub API integration, use the `--app-id` and `--private-key` options with valid credentials
