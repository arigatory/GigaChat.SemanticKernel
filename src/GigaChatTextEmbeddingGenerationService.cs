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
#pragma warning disable CS0618 // Type or member is obsolete

namespace GigaChat.SemanticKernel;

/// <summary>
/// GigaChat Text Embedding Generation Service.
/// This service is deprecated. Use Microsoft.Extensions.AI.IEmbeddingGenerator instead.
/// </summary>
[Obsolete("Use Microsoft.Extensions.AI.IEmbeddingGenerator<string, Embedding<float>> instead. This will be removed in a future version.")]
public sealed class GigaChatTextEmbeddingGenerationService : ITextEmbeddingGenerationService, IDisposable
{
    private readonly GigaChatClient _client;
    private readonly string _modelId;
    private readonly Dictionary<string, object?> _attributes;
    private const int MaxBatchSize = 100; // GigaChat API limit per request

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
        var allEmbeddings = new List<ReadOnlyMemory<float>>();

        // Split into batches to avoid 413 Request Entity Too Large error
        for (int i = 0; i < data.Count; i += MaxBatchSize)
        {
            var batch = data.Skip(i).Take(MaxBatchSize).ToList();
            
            var request = new GigaChatEmbeddingsRequest
            {
                Model = _modelId,
                Input = batch
            };

            var response = await _client.CreateEmbeddingsAsync(request, cancellationToken);

            var embeddings = new List<ReadOnlyMemory<float>>();
            foreach (var embeddingData in response.Data.OrderBy(d => d.Index))
            {
                embeddings.Add(new ReadOnlyMemory<float>(embeddingData.Embedding));
            }

            allEmbeddings.AddRange(embeddings);
        }

        return allEmbeddings;
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}

#pragma warning restore SKEXP0001
