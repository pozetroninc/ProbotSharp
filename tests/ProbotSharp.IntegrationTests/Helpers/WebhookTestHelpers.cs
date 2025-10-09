// Copyright (c) ProbotSharp Contributors.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;

namespace ProbotSharp.IntegrationTests.Helpers;

/// <summary>
/// Helper methods for creating and manipulating webhook test data.
/// </summary>
public static class WebhookTestHelpers
{
    public static string GenerateWebhookSignature(string payload, string secret)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(secret);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var signature = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
        return signature;
    }

    public static string CreatePushEventPayload(long installationId = 12345)
    {
        return $$"""
        {
          "ref": "refs/heads/main",
          "before": "0000000000000000000000000000000000000000",
          "after": "1111111111111111111111111111111111111111",
          "repository": {
            "id": 1,
            "name": "test-repo",
            "full_name": "test-org/test-repo",
            "private": false,
            "owner": {
              "login": "test-org",
              "id": 1
            }
          },
          "pusher": {
            "name": "test-user",
            "email": "test@example.com"
          },
          "sender": {
            "login": "test-user",
            "id": 1
          },
          "installation": {
            "id": {{installationId}}
          }
        }
        """;
    }

    public static string CreateIssuesEventPayload(long installationId = 12345, string action = "opened")
    {
        return $$"""
        {
          "action": "{{action}}",
          "issue": {
            "number": 1,
            "title": "Test Issue",
            "body": "This is a test issue",
            "state": "open",
            "user": {
              "login": "test-user",
              "id": 1
            }
          },
          "repository": {
            "id": 1,
            "name": "test-repo",
            "full_name": "test-org/test-repo",
            "private": false,
            "owner": {
              "login": "test-org",
              "id": 1
            }
          },
          "sender": {
            "login": "test-user",
            "id": 1
          },
          "installation": {
            "id": {{installationId}}
          }
        }
        """;
    }

    public static string CreatePullRequestEventPayload(long installationId = 12345, string action = "opened")
    {
        return $$"""
        {
          "action": "{{action}}",
          "number": 1,
          "pull_request": {
            "id": 1,
            "number": 1,
            "state": "open",
            "title": "Test Pull Request",
            "body": "This is a test PR",
            "user": {
              "login": "test-user",
              "id": 1
            },
            "head": {
              "ref": "feature-branch",
              "sha": "1111111111111111111111111111111111111111"
            },
            "base": {
              "ref": "main",
              "sha": "0000000000000000000000000000000000000000"
            }
          },
          "repository": {
            "id": 1,
            "name": "test-repo",
            "full_name": "test-org/test-repo",
            "private": false,
            "owner": {
              "login": "test-org",
              "id": 1
            }
          },
          "sender": {
            "login": "test-user",
            "id": 1
          },
          "installation": {
            "id": {{installationId}}
          }
        }
        """;
    }

    public static string CreateIssueCommentEventPayload(long installationId = 12345, string action = "created")
    {
        return $$"""
        {
          "action": "{{action}}",
          "issue": {
            "number": 1,
            "title": "Test Issue",
            "state": "open",
            "user": {
              "login": "test-user",
              "id": 1
            }
          },
          "comment": {
            "id": 1,
            "body": "This is a test comment",
            "user": {
              "login": "test-user",
              "id": 1
            },
            "created_at": "2024-01-01T00:00:00Z"
          },
          "repository": {
            "id": 1,
            "name": "test-repo",
            "full_name": "test-org/test-repo",
            "private": false,
            "owner": {
              "login": "test-org",
              "id": 1
            }
          },
          "sender": {
            "login": "test-user",
            "id": 1
          },
          "installation": {
            "id": {{installationId}}
          }
        }
        """;
    }

    public static string CreateMalformedPayload()
    {
        return """
        {
          "invalid": "json",
          "missing_installation": true,
          "no_repository": {
        """;
    }

    public static string CreatePayloadWithMissingInstallation()
    {
        return """
        {
          "ref": "refs/heads/main",
          "repository": {
            "id": 1,
            "name": "test-repo",
            "full_name": "test-org/test-repo"
          },
          "sender": {
            "login": "test-user",
            "id": 1
          }
        }
        """;
    }

    public static HttpRequestMessage CreateWebhookRequest(
        string payload,
        string signature,
        string eventName = "push",
        string deliveryId = "12345678-1234-1234-1234-123456789012")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("X-GitHub-Event", eventName);
        request.Headers.Add("X-GitHub-Delivery", deliveryId);
        request.Headers.Add("X-Hub-Signature-256", signature);

        return request;
    }
}
