using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GigaChat.SemanticKernel;

public sealed class GigaChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelId;

    public GigaChatClient(HttpClient httpClient, string apiKey, string modelId, string? endpoint)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _modelId = modelId;
        _httpClient.BaseAddress = new Uri(endpoint ?? "https://gigachat.devices.sberbank.ru/api/v1");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    // Add methods for Chat, Embeddings, etc. using API documentation
    // Example method for chat completions:
    public async Task<string> CreateChatCompletionAsync(object request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/chat/completions", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}