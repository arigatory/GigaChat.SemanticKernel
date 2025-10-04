using Microsoft.Extensions.AI;
using GigaChat.SemanticKernel.Models;

namespace ChatApp.Rag.GigaChat.Services;

/// <summary>
/// Adapter that wraps GigaChat embedding service to work with Microsoft.Extensions.AI
/// </summary>
public sealed class GigaChatEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly GigaChat.SemanticKernel.GigaChatClient _client;
    private readonly string _modelId;

    public GigaChatEmbeddingGenerator(string authorizationKey, string modelId = "Embeddings")
    {
        _client = new GigaChat.SemanticKernel.GigaChatClient(authorizationKey, modelId);
        _modelId = modelId;
    }

    public EmbeddingGeneratorMetadata Metadata => new EmbeddingGeneratorMetadata(
        "GigaChat", 
        _modelId, 
        1024  // GigaChat Embeddings dimension is 1024
    );

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var request = new GigaChatEmbeddingsRequest
        {
            Model = _modelId,
            Input = values.ToList()
        };

        var response = await _client.CreateEmbeddingsAsync(request, cancellationToken);

        var embeddings = response.Data
            .OrderBy(d => d.Index)
            .Select(d => new Embedding<float>(d.Embedding))
            .ToList();

        return new GeneratedEmbeddings<Embedding<float>>(embeddings);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }
}
