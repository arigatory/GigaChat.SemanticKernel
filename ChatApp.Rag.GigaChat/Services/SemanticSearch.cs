using Microsoft.Extensions.VectorData;

namespace ChatApp.Rag.GigaChat.Services;

public class SemanticSearch(
    ILogger<SemanticSearch> logger,
    VectorStoreCollection<string, IngestedChunk> vectorCollection)
{
    public async Task<IReadOnlyList<IngestedChunk>> SearchAsync(string text, string? documentIdFilter, int maxResults)
    {
        logger.LogInformation("Searching for: '{text}', documentIdFilter: '{filter}', maxResults: {maxResults}", 
            text, documentIdFilter ?? "none", maxResults);
        
        var nearest = vectorCollection.SearchAsync(text, maxResults, new VectorSearchOptions<IngestedChunk>
        {
            Filter = documentIdFilter is { Length: > 0 } ? record => record.DocumentId == documentIdFilter : null,
        });

        var results = await nearest.Select(result => result.Record).ToListAsync();
        
        logger.LogInformation("Found {count} results for search '{text}'", results.Count, text);
        foreach (var result in results)
        {
            logger.LogInformation("Result: DocumentId={documentId}, Page={page}, Text={textPreview}...", 
                result.DocumentId, result.PageNumber, result.Text.Length > 100 ? result.Text.Substring(0, 100) : result.Text);
        }
        
        return results;
    }
}
