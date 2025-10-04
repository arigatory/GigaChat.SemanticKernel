using Microsoft.Extensions.AI;

namespace ChatApp.Rag.GigaChat.Services;

/// <summary>
/// Simple embedding generator for demo purposes.
/// In production, use proper embedding models from OpenAI, Azure, or other providers.
/// </summary>
public class SimpleEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly Random _random = new Random(42); // Fixed seed for consistency

    public EmbeddingGeneratorMetadata Metadata => new("SimpleEmbedding");

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<Embedding<float>>();
        
        foreach (var value in values)
        {
            // Generate a simple hash-based embedding
            var embedding = GenerateSimpleEmbedding(value);
            embeddings.Add(new Embedding<float>(embedding));
        }

        return await Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    private float[] GenerateSimpleEmbedding(string text)
    {
        // Simple hash-based embedding generation
        // This is NOT suitable for production - use proper embedding models
        var hash = text.GetHashCode();
        var random = new Random(hash);
        
        var embedding = new float[1536];
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2 - 1); // Range: -1 to 1
        }

        // Normalize the vector
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] /= magnitude;
        }

        return embedding;
    }
}
