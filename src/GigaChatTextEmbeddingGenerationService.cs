using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GigaChat.SemanticKernel.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

#nullable enable
#pragma warning disable SKEXP0001 // ITextEmbeddingGenerationService is experimental

namespace GigaChat.SemanticKernel;

public sealed class GigaChatTextEmbeddingGenerationService : ITextEmbeddingGenerationService, IDisposable
{
    private readonly GigaChatClient _client;
    private readonly string _modelId;
    private readonly Dictionary<string, object?> _attributes;

    public IReadOnlyDictionary<string, object?> Attributes => _attributes;

    public GigaChatTextEmbeddingGenerationService(
        string authorizationKey,
        string modelId = "Embeddings",
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

    public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IList<string> data,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var request = new GigaChatEmbeddingsRequest
        {
            Model = _modelId,
            Input = data.ToList()
        };

        var response = await _client.CreateEmbeddingsAsync(request, cancellationToken);

        var embeddings = new List<ReadOnlyMemory<float>>();
        foreach (var embeddingData in response.Data.OrderBy(d => d.Index))
        {
            embeddings.Add(new ReadOnlyMemory<float>(embeddingData.Embedding));
        }

        return embeddings;
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}

#pragma warning restore SKEXP0001
