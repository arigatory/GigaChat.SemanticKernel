using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextGeneration;

namespace GigaChat.SemanticKernel;

internal class GigaChatTextGenerationService : ITextGenerationService
{
    private string apiKey;
    private string modelId;
    private string endpoint;
    private HttpClient httpClient;

    public GigaChatTextGenerationService(string apiKey, string modelId, string endpoint, HttpClient httpClient)
    {
        this.apiKey = apiKey;
        this.modelId = modelId;
        this.endpoint = endpoint;
        this.httpClient = httpClient;
    }

    public IReadOnlyDictionary<string, object> Attributes => throw new System.NotImplementedException();

    public IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(string prompt, PromptExecutionSettings executionSettings = null, Kernel kernel = null, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(string prompt, PromptExecutionSettings executionSettings = null, Kernel kernel = null, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }
}