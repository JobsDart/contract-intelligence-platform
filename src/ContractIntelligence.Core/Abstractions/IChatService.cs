namespace ContractIntelligence.Core.Abstractions;

/// <summary>
/// Thin abstraction over a chat completion model (Azure OpenAI GPT-4o).
/// Kept deliberately small so the domain never depends on Semantic Kernel types.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Run a single-turn completion.
    /// </summary>
    /// <param name="systemPrompt">Instructions that set the model's behaviour.</param>
    /// <param name="userPrompt">The user / task content.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
