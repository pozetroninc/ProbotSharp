# CompatibilityTestBot

A specialized test application for verifying **Probot Sharp** compatibility with **Probot (Node.js)**.

## Purpose

This bot exists solely for integration testing. It tracks all received webhook events in memory and exposes a test API for verification, enabling the integration test suite at `/home/neil/Documents/Projects/probot-integration-tests` to verify that Probot Sharp correctly handles GitHub webhooks.

## Features

- ✅ **Webhook Reception**: Accepts webhooks at `/webhooks` and `/api/github/webhooks`
- ✅ **Signature Validation**: HMAC-SHA256 webhook signature verification
- ✅ **Event Tracking**: In-memory storage of all received webhook events
- ✅ **Test API**: Query and filter tracked events via HTTP endpoints
- ✅ **Probot Compatibility**: `/ping` endpoint matches Probot (Node.js) behavior
- ✅ **Zero Infrastructure**: Pure in-memory mode (no database, no Redis)

## Endpoints

### Webhook Endpoints

- `POST /webhooks` - Primary webhook endpoint (ProbotSharp convention)
- `POST /api/github/webhooks` - Alternate endpoint (Probot Node.js compatibility)

### Test API Endpoints

- `GET /test/events` - Get all tracked events
- `GET /test/events/{eventName}` - Get events filtered by event name (e.g., `/test/events/push`)
- `DELETE /test/events` - Clear all tracked events

### Health Endpoints

- `GET /health` - Health check endpoint
- `GET /ping` - Probot-compatible ping endpoint
- `GET /` - Application metadata

## Configuration

**File:** `appsettings.json`

```json
{
  "ProbotSharp": {
    "AppId": "123456",
    "WebhookSecret": "secret",
    "PrivateKeyPath": "test-private-key.pem"
  }
}
```

**Important:** The `WebhookSecret` is hardcoded to `"secret"` to match the integration test suite expectations.

## Running Locally

```bash
# Build
dotnet build

# Run on default port (5000)
dotnet run

# Run on custom port
dotnet run --urls http://localhost:5123
```

## Sending Test Webhooks

```bash
# Create test payload
cat > /tmp/test-webhook.json << 'EOF'
{
  "action": "opened",
  "issue": {
    "number": 1,
    "title": "Test Issue"
  },
  "repository": {
    "name": "test-repo",
    "owner": { "login": "test-owner" }
  }
}
EOF

# Generate signature
SECRET="secret"
BODY=$(cat /tmp/test-webhook.json)
SIGNATURE="sha256=$(echo -n "$BODY" | openssl dgst -sha256 -hmac "$SECRET" | cut -d' ' -f2)"

# Send webhook
curl -X POST http://localhost:5000/webhooks \
  -H "Content-Type: application/json" \
  -H "X-GitHub-Event: issues" \
  -H "X-GitHub-Delivery: test-123" \
  -H "X-Hub-Signature-256: $SIGNATURE" \
  -d @/tmp/test-webhook.json

# Verify it was tracked
curl http://localhost:5000/test/events | jq
```

## Integration Tests

This bot is consumed by the integration test suite at:
`/home/neil/Documents/Projects/probot-integration-tests`

### Running Integration Tests

```bash
cd /home/neil/Documents/Projects/probot-integration-tests

# Build this bot first
cd /home/neil/Documents/Projects/probot-sharp/examples/CompatibilityTestBot
dotnet build

# Run integration tests
cd /home/neil/Documents/Projects/probot-integration-tests
npm test -- test/probot-sharp
```

## Test Event Structure

Tracked events have the following structure:

```typescript
{
  eventName: string;      // e.g., "issues", "push", "pull_request"
  action: string | null;  // e.g., "opened", "closed", null for push events
  deliveryId: string;     // GitHub webhook delivery ID
  payload: object;        // Full webhook payload
  receivedAt: string;     // ISO 8601 timestamp
}
```

## Event Handlers

The bot includes generic event handlers for:

- `push` events (no action)
- `issues` events (all actions: opened, closed, reopened, edited, etc.)
- `pull_request` events (all actions: opened, closed, synchronized, etc.)
- `issue_comment` events (created, edited, deleted)
- `check_run` events (created, completed, rerequested, requested_action)
- `check_suite` events (completed, requested, rerequested)

All handlers simply track the event rather than performing actual GitHub operations.

## Architecture

```
┌──────────────────────────────┐
│   Integration Tests          │
│   (Vitest / Node.js)         │
│   - Starts bot process       │
│   - Sends signed webhooks     │
│   - Queries /test/events      │
└──────────┬───────────────────┘
           │ HTTP
           ↓
┌──────────────────────────────┐
│   CompatibilityTestBot       │
│   (ASP.NET Core / .NET 8)    │
│   - Validates signatures      │
│   - Routes to event handlers  │
│   - Tracks events in memory   │
│   - Exposes test API          │
└──────────────────────────────┘
```

## Differences from Production Bots

| Aspect | Production Bots | CompatibilityTestBot |
|--------|----------------|----------------------|
| **Purpose** | Handle real webhooks, call GitHub API | Track events for testing |
| **Event Handlers** | Perform actions (create comments, labels, etc.) | Only track events |
| **Persistence** | May use database/Redis | Pure in-memory |
| **Test API** | No test endpoints | Full test API (`/test/events`) |
| **Configuration** | Dynamic webhook secret | Hardcoded `"secret"` |

## Development

When adding support for new event types:

1. Add handler class in `CompatibilityTestApp.cs`
2. Register handler in `InitializeAsync()`
3. Handler should inject `TestEventTracker` and call `AddEvent()`
4. Rebuild and test

Example:

```text
public class GenericNewEventHandler : IEventHandler
{
    private readonly TestEventTracker _eventTracker;
    private readonly ILogger<GenericNewEventHandler> _logger;

    public GenericNewEventHandler(TestEventTracker eventTracker, ILogger<GenericNewEventHandler> logger)
    {
        _eventTracker = eventTracker;
        _logger = logger;
    }

    public Task HandleAsync(ProbotSharpContext context, CancellationToken ct = default)
    {
        var action = context.Payload["action"]?.ToString();
        _logger.LogInformation("Handling new_event.{Action}", action);

        _eventTracker.AddEvent(new TrackedEvent
        {
            EventName = context.EventName,
            Action = action,
            DeliveryId = context.Id,
            Payload = context.Payload,
            ReceivedAt = DateTime.UtcNow,
        });

        return Task.CompletedTask;
    }
}
```

## Troubleshooting

### Integration tests fail with "connection refused"

**Cause:** Bot not running or port mismatch

**Solution:**
```bash
# Build first
dotnet build

# Check if port is in use
lsof -i :5000
```

### Webhook signature validation fails

**Cause:** Secret mismatch

**Solution:** Ensure `appsettings.json` has `"WebhookSecret": "secret"` (matches integration test constant)

### Events not appearing in /test/events

**Possible causes:**
1. Event handler not registered
2. Signature validation failed
3. Wrong event name

**Debug:**
```bash
# Check logs when sending webhook
dotnet run --urls http://localhost:5000

# In another terminal, send webhook and check logs
```

## Related Documentation

- [ProbotSharp Architecture](../../docs/Architecture.md)
- [Probot-to-ProbotSharp Migration Guide](../../docs/Probot-to-ProbotSharp-Guide.md)
- [Configuration Best Practices](../../docs/ConfigurationBestPractices.md)

---

**Last Updated:** 2025-10-23
**Status:** ✅ Implemented
**Purpose:** Integration testing only (not for production use)
