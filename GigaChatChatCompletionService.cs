using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GigaChat.SemanticKernel;

public sealed class GigaChatChatCompletionService : IChatCompletionService
{
    private readonly GigaChatClient _client;
    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

    public GigaChatChatCompletionService(
        string apiKey, 
        string modelId,
        string? endpoint = null,
        HttpClient? httpClient = null)
    {
        _client = new GigaChatClient(httpClient ?? new HttpClient(), apiKey, modelId, endpoint);
    }

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        // Implement using _client.CreateChatCompletionAsync()
        throw new NotImplementedException("GetChatMessageContentsAsync is not implemented yet.");
    }

    public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings executionSettings = null, Kernel kernel = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    // Implement streaming if supported
}