using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GigaChat.SemanticKernel.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

#nullable enable

namespace GigaChat.SemanticKernel;

internal sealed class GigaChatTextGenerationService : ITextGenerationService, IDisposable
{
    private readonly GigaChatClient _client;
    private readonly string _modelId;
    private readonly Dictionary<string, object?> _attributes;

    public IReadOnlyDictionary<string, object?> Attributes => _attributes;

    public GigaChatTextGenerationService(
        string authorizationKey, 
        string modelId,
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

    public async Task<IReadOnlyList<TextContent>> GetTextContentsAsync(
        string prompt, 
        PromptExecutionSettings? executionSettings = null, 
        Kernel? kernel = null, 
        CancellationToken cancellationToken = default)
    {
        var request = CreateGigaChatRequest(prompt, executionSettings);
        var response = await _client.CreateChatCompletionAsync(request, cancellationToken);

        var results = new List<TextContent>();
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

            var content = new TextContent(
                text: choice.Message.Content,
                modelId: _modelId,
                innerContent: response,
                metadata: metadata
            );

            results.Add(content);
        }

        return results;
    }

    public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(
        string prompt,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = CreateGigaChatRequest(prompt, executionSettings);
        
        await foreach (var streamResponse in _client.StreamChatCompletionAsync(request, cancellationToken))
        {
            foreach (var choice in streamResponse.Choices)
            {
                if (choice.Delta?.Content == null)
                    continue;

                var metadata = new Dictionary<string, object?>
                {
                    ["FinishReason"] = choice.FinishReason,
                    ["Index"] = choice.Index
                };

                yield return new StreamingTextContent(
                    text: choice.Delta.Content,
                    modelId: _modelId,
                    innerContent: streamResponse,
                    metadata: metadata
                );
            }
        }
    }

    private GigaChatRequest CreateGigaChatRequest(
        string prompt,
        PromptExecutionSettings? executionSettings)
    {
        var request = new GigaChatRequest
        {
            Model = _modelId,
            Messages = new List<GigaChatMessage>
            {
                new() { Role = "user", Content = prompt }
            }
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

    public void Dispose()
    {
        _client.Dispose();
    }
}