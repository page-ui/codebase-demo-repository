namespace Page.Ui.Worker.Ai.Models;

public sealed class AiModelDispatchResult
{
    public bool Accepted { get; init; }
    public string? FailureMessage { get; init; }

    public static AiModelDispatchResult Success()
    {
        return new AiModelDispatchResult
        {
            Accepted = true
        };
    }

    public static AiModelDispatchResult Failed(string message)
    {
        return new AiModelDispatchResult
        {
            Accepted = false,
            FailureMessage = message
        };
    }
}
