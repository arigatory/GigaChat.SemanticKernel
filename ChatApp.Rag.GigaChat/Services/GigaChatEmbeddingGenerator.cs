using Microsoft.Extensions.AI;
using GigaChat.SemanticKernel.Models;
using GigaChatClient = global::GigaChat.SemanticKernel.GigaChatClient;

namespace ChatApp.Rag.GigaChat.Services;

/// <summary>
/// Adapter that wraps GigaChat embedding service to work with Microsoft.Extensions.AI
/// </summary>
public sealed class GigaChatEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly GigaChatClient _client;
    private readonly string _modelId;
    private const int MaxBatchSize = 100; // GigaChat API limit per request
    private const int MaxTokensPerText = 500; // GigaChat limit is 514, using 500 for safety margin
    private const int ApproxCharsPerToken = 4; // Approximate characters per token for truncation

    public GigaChatEmbeddingGenerator(string authorizationKey, string modelId = "Embeddings")
    {
        _client = new GigaChatClient(authorizationKey, modelId);
        _modelId = modelId;
    }

    public EmbeddingGeneratorMetadata Metadata => new("GigaChat");

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var valuesList = values.ToList();
        var allEmbeddings = new List<Embedding<float>>();

        // Truncate texts that are too long to avoid 413 errors
        var processedValues = valuesList.Select(text =>
        {
            var maxChars = MaxTokensPerText * ApproxCharsPerToken;
            if (text.Length > maxChars)
            {
                // Truncate long texts to stay under token limit
                return text.Substring(0, maxChars);
            }
            return text;
        }).ToList();

        // Split into batches to avoid 413 Request Entity Too Large error
        for (int i = 0; i < processedValues.Count; i += MaxBatchSize)
        {
            var batch = processedValues.Skip(i).Take(MaxBatchSize).ToList();
            
            var request = new GigaChatEmbeddingsRequest
            {
                Model = _modelId,
                Input = batch
            };

            var response = await _client.CreateEmbeddingsAsync(request, cancellationToken);

            var embeddings = response.Data
                .OrderBy(d => d.Index)
                .Select(d => new Embedding<float>(d.Embedding))
                .ToList();

            allEmbeddings.AddRange(embeddings);
        }

        return new GeneratedEmbeddings<Embedding<float>>(allEmbeddings);
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
