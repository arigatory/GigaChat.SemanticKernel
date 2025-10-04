using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Runtime.CompilerServices;

namespace ChatApp.Rag.GigaChat.Services;

/// <summary>
/// Adapter that wraps GigaChat Semantic Kernel service to work with Microsoft.Extensions.AI
/// </summary>
public sealed class GigaChatAIChatClient : IChatClient
{
    private readonly IChatCompletionService _chatCompletion;
    private readonly string _modelId;

    public GigaChatAIChatClient(IChatCompletionService chatCompletion, string modelId)
    {
        _chatCompletion = chatCompletion;
        _modelId = modelId;
    }

    public ChatClientMetadata Metadata => new ChatClientMetadata(providerName: "GigaChat", modelId: _modelId);

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var chatHistory = ConvertToChatHistory(chatMessages);
        var settings = ConvertToExecutionSettings(options);

        var result = await _chatCompletion.GetChatMessageContentsAsync(
            chatHistory,
            settings,
            cancellationToken: cancellationToken);

        var message = result.FirstOrDefault();
        if (message == null)
        {
            throw new InvalidOperationException("No response from GigaChat");
        }

        return new ChatResponse(new[]
        {
            new ChatMessage(ChatRole.Assistant, message.Content)
        });
    }

    public async IAsyncEnumerable<StreamingChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatHistory = ConvertToChatHistory(chatMessages);
        var settings = ConvertToExecutionSettings(options);

        await foreach (var item in _chatCompletion.GetStreamingChatMessageContentsAsync(
            chatHistory,
            settings,
            cancellationToken: cancellationToken))
        {
            yield return new StreamingChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Text = item.Content
            };
        }
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(IChatCompletionService))
        {
            return _chatCompletion;
        }
        return null;
    }

    private static Microsoft.SemanticKernel.ChatCompletion.ChatHistory ConvertToChatHistory(
        IEnumerable<ChatMessage> chatMessages)
    {
        var history = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
        
        foreach (var message in chatMessages)
        {
            var role = message.Role.Value switch
            {
                "user" => AuthorRole.User,
                "assistant" => AuthorRole.Assistant,
                "system" => AuthorRole.System,
                _ => AuthorRole.User
            };

            var content = string.Join("", message.Contents.OfType<TextContent>().Select(c => c.Text));
            history.AddMessage(role, content);
        }

        return history;
    }

    private static PromptExecutionSettings? ConvertToExecutionSettings(ChatOptions? options)
    {
        if (options == null)
        {
            return null;
        }

        var settings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>()
        };

        if (options.Temperature.HasValue)
        {
            settings.ExtensionData["temperature"] = options.Temperature.Value;
        }

        if (options.TopP.HasValue)
        {
            settings.ExtensionData["top_p"] = options.TopP.Value;
        }

        if (options.MaxOutputTokens.HasValue)
        {
            settings.ExtensionData["max_tokens"] = options.MaxOutputTokens.Value;
        }

        return settings;
    }
}
