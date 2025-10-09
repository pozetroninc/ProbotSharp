namespace ConfigBot;

/// <summary>
/// Configuration settings loaded from .github/configbot.yml
/// </summary>
public class ConfigBotSettings
{
    public string WelcomeMessage { get; set; } = "Welcome! Thanks for opening an issue.";
    public bool EnableAutoLabel { get; set; } = true;
    public List<string> DefaultLabels { get; set; } = new();
    public int MaxCommentsPerIssue { get; set; } = 10;
}
