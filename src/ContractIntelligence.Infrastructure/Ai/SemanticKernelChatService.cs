using ContractIntelligence.Core.Abstractions;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ContractIntelligence.Infrastructure.Ai;

/// <summary>
/// Adapts Semantic Kernel's <see cref="IChatCompletionService"/> to our small,
/// dependency-free <see cref="IChatService"/> abstraction. Temperature is kept low
/// because contract analysis must be deterministic and faithful, not creative.
/// </summary>
public sealed class SemanticKernelChatService : IChatService
{
    private readonly IChatCompletionService _chat;

    private static readonly OpenAIPromptExecutionSettings Settings = new()
    {
        Temperature = 0.1,
        MaxTokens = 1500
    };

    public SemanticKernelChatService(IChatCompletionService chat) => _chat = chat;

    public async Task<string> CompleteAsync(
        string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(userPrompt);

        var response = await _chat.GetChatMessageContentAsync(history, Settings, kernel: null, ct);
        return response.Content ?? string.Empty;
    }
}
