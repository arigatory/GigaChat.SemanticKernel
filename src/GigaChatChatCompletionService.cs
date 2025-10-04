using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GigaChat.SemanticKernel.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

#nullable enable

namespace GigaChat.SemanticKernel;

public sealed class GigaChatChatCompletionService : IChatCompletionService, IDisposable
{
    private readonly GigaChatClient _client;
    private readonly string _modelId;
    private readonly Dictionary<string, object?> _attributes;

    public IReadOnlyDictionary<string, object?> Attributes => _attributes;

    public GigaChatChatCompletionService(
        string authorizationKey, 
        string modelId = "GigaChat",
        string? endpoint = null,
        HttpClient? httpClient = null)
    {
        _modelId = modelId;
        _client = new GigaChatClient(authorizationKey, modelId, endpoint, httpClient);
        _attributes = new Dictionary<string, object?>
        {
            ["ModelId"] = modelId
        };
    }

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var request = CreateGigaChatRequest(chatHistory, executionSettings);
        var response = await _client.CreateChatCompletionAsync(request, cancellationToken);

        var results = new List<ChatMessageContent>();
        foreach (var choice in response.Choices)
        {
            if (choice.Message == null)
                continue;

            var metadata = new Dictionary<string, object?>
            {
                ["FinishReason"] = choice.FinishReason,
                ["Index"] = choice.Index
            };

            if (response.Usage != null)
            {
                metadata["Usage"] = new
                {
                    response.Usage.PromptTokens,
                    response.Usage.CompletionTokens,
                    response.Usage.TotalTokens
                };
            }

            var content = new ChatMessageContent(
                role: MapRoleToAuthorRole(choice.Message.Role),
                content: choice.Message.Content,
                modelId: _modelId,
                innerContent: response,
                metadata: metadata
            );

            results.Add(content);
        }

        return results;
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = CreateGigaChatRequest(chatHistory, executionSettings);
        
        await foreach (var streamResponse in _client.StreamChatCompletionAsync(request, cancellationToken))
        {
            foreach (var choice in streamResponse.Choices)
            {
                if (choice.Delta == null)
                    continue;

                var metadata = new Dictionary<string, object?>
                {
                    ["FinishReason"] = choice.FinishReason,
                    ["Index"] = choice.Index
                };

                AuthorRole? role = null;
                if (!string.IsNullOrEmpty(choice.Delta.Role))
                {
                    role = MapRoleToAuthorRole(choice.Delta.Role);
                }

                yield return new StreamingChatMessageContent(
                    role: role,
                    content: choice.Delta.Content,
                    innerContent: streamResponse,
                    modelId: _modelId,
                    metadata: metadata
                );
            }
        }
    }

    private GigaChatRequest CreateGigaChatRequest(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings)
    {
        var request = new GigaChatRequest
        {
            Model = _modelId,
            Messages = chatHistory.Select(m => new GigaChatMessage
            {
                Role = MapAuthorRoleToRole(m.Role),
                Content = m.Content ?? string.Empty
            }).ToList()
        };

        if (executionSettings != null)
        {
            if (executionSettings.ExtensionData?.TryGetValue("temperature", out var temp) == true)
                request.Temperature = Convert.ToDouble(temp);

            if (executionSettings.ExtensionData?.TryGetValue("top_p", out var topP) == true)
                request.TopP = Convert.ToDouble(topP);

            if (executionSettings.ExtensionData?.TryGetValue("max_tokens", out var maxTokens) == true)
                request.MaxTokens = Convert.ToInt32(maxTokens);

            if (executionSettings.ExtensionData?.TryGetValue("repetition_penalty", out var repPenalty) == true)
                request.RepetitionPenalty = Convert.ToDouble(repPenalty);
        }

        return request;
    }

    private static string MapAuthorRoleToRole(AuthorRole role)
    {
        return role.Label.ToLowerInvariant() switch
        {
            "user" => "user",
            "assistant" => "assistant",
            "system" => "system",
            _ => "user"
        };
    }

    private static AuthorRole MapRoleToAuthorRole(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "user" => AuthorRole.User,
            "assistant" => AuthorRole.Assistant,
            "system" => AuthorRole.System,
            _ => AuthorRole.User
        };
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}