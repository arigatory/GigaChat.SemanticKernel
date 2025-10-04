using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace ChatApp.Rag.GigaChat.Services.Ingestion;

public class DataIngestor(
    ILogger<DataIngestor> logger,
    VectorStoreCollection<string, IngestedChunk> chunksCollection,
    VectorStoreCollection<string, IngestedDocument> documentsCollection)
{
    public static async Task IngestDataAsync(IServiceProvider services, IIngestionSource source)
    {
        using var scope = services.CreateScope();
        var ingestor = scope.ServiceProvider.GetRequiredService<DataIngestor>();
        await ingestor.IngestDataAsync(source);
    }

    public async Task IngestDataAsync(IIngestionSource source)
    {
        await chunksCollection.EnsureCollectionExistsAsync();
        await documentsCollection.EnsureCollectionExistsAsync();

        var sourceId = source.SourceId;
        logger.LogInformation("Starting ingestion from source: {sourceId}", sourceId);
        
        var documentsForSource = await documentsCollection.GetAsync(doc => doc.SourceId == sourceId, top: int.MaxValue).ToListAsync();
        logger.LogInformation("Found {count} existing documents in source", documentsForSource.Count);

        var deletedDocuments = await source.GetDeletedDocumentsAsync(documentsForSource);
        foreach (var deletedDocument in deletedDocuments)
        {
            logger.LogInformation("Removing ingested data for {documentId}", deletedDocument.DocumentId);
            await DeleteChunksForDocumentAsync(deletedDocument);
            await documentsCollection.DeleteAsync(deletedDocument.Key);
        }

        var modifiedDocuments = await source.GetNewOrModifiedDocumentsAsync(documentsForSource);
        logger.LogInformation("Found {count} new or modified documents to process", modifiedDocuments.Count());
        
        foreach (var modifiedDocument in modifiedDocuments)
        {
            logger.LogInformation("Processing {documentId}", modifiedDocument.DocumentId);
            await DeleteChunksForDocumentAsync(modifiedDocument);

            await documentsCollection.UpsertAsync(modifiedDocument);

            var newRecords = await source.CreateChunksForDocumentAsync(modifiedDocument);
            var recordsList = newRecords.ToList();
            logger.LogInformation("Created {count} chunks for {documentId}", recordsList.Count, modifiedDocument.DocumentId);
            
            await chunksCollection.UpsertAsync(recordsList);
            logger.LogInformation("Successfully indexed {count} chunks for {documentId}", recordsList.Count, modifiedDocument.DocumentId);
        }

        logger.LogInformation("Ingestion is up-to-date");

        async Task DeleteChunksForDocumentAsync(IngestedDocument document)
        {
            var documentId = document.DocumentId;
            var chunksToDelete = await chunksCollection.GetAsync(record => record.DocumentId == documentId, int.MaxValue).ToListAsync();
            if (chunksToDelete.Any())
            {
                logger.LogInformation("Deleting {count} old chunks for {documentId}", chunksToDelete.Count, documentId);
                await chunksCollection.DeleteAsync(chunksToDelete.Select(r => r.Key));
            }
        }
    }
}
