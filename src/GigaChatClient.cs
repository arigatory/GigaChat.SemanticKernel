using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GigaChat.SemanticKernel.Models;

#nullable enable

namespace GigaChat.SemanticKernel;

public sealed class GigaChatClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _authorizationKey;
    private readonly string _modelId;
    private readonly string _apiEndpoint;
    private readonly string _tokenEndpoint;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private readonly bool _disposeHttpClient;
    
    private string? _accessToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    public GigaChatClient(
        string authorizationKey, 
        string modelId, 
        string? apiEndpoint = null,
        HttpClient? httpClient = null)
    {
        _authorizationKey = authorizationKey;
        _modelId = modelId;
        _apiEndpoint = apiEndpoint ?? "https://gigachat.devices.sberbank.ru/api/v1";
        _tokenEndpoint = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
        
        if (httpClient == null)
        {
            _httpClient = new HttpClient();
            _disposeHttpClient = true;
        }
        else
        {
            _httpClient = httpClient;
            _disposeHttpClient = false;
        }
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // Check if token is still valid (with 1 minute buffer)
        if (_accessToken != null && _tokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return _accessToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            // Double check after acquiring lock
            if (_accessToken != null && _tokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return _accessToken;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authorizationKey);
            request.Headers.Add("RqUID", Guid.NewGuid().ToString());
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
            });
            request.Content = content;

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<GigaChatTokenResponse>(cancellationToken: cancellationToken);
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("Failed to obtain access token from GigaChat");
            }

            _accessToken = tokenResponse.AccessToken;
            // expires_at is in milliseconds according to API specification
            _tokenExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(tokenResponse.ExpiresAt);

            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task<GigaChatResponse> CreateChatCompletionAsync(
        GigaChatRequest request, 
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_apiEndpoint}/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        var json = JsonSerializer.Serialize(request);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GigaChatResponse>(cancellationToken: cancellationToken);
        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize GigaChat response");
        }

        return result;
    }

    public async IAsyncEnumerable<GigaChatStreamResponse> StreamChatCompletionAsync(
        GigaChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_apiEndpoint}/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        request.Stream = true;
        var json = JsonSerializer.Serialize(request);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:"))
            {
                continue;
            }

            var data = line.Substring(5).Trim();
            if (data == "[DONE]")
            {
                break;
            }

            GigaChatStreamResponse? streamResponse;
            try
            {
                streamResponse = JsonSerializer.Deserialize<GigaChatStreamResponse>(data);
            }
            catch
            {
                continue;
            }

            if (streamResponse != null)
            {
                yield return streamResponse;
            }
        }
    }

    public async Task<GigaChatEmbeddingsResponse> CreateEmbeddingsAsync(
        GigaChatEmbeddingsRequest request,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_apiEndpoint}/embeddings");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        var json = JsonSerializer.Serialize(request);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"GigaChat embeddings request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}). " +
                $"Input count: {request.Input.Count}. " +
                $"Consider reducing batch size if you get 413 error. " +
                $"Response: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<GigaChatEmbeddingsResponse>(cancellationToken: cancellationToken);
        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize GigaChat embeddings response");
        }

        return result;
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
        _tokenLock.Dispose();
    }
}