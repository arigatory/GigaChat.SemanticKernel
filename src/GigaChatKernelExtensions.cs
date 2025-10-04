using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.TextGeneration;

#nullable enable

namespace GigaChat.SemanticKernel;

public static class GigaChatKernelExtensions
{
    /// <summary>
    /// Adds GigaChat chat completion and text generation services to the kernel.
    /// </summary>
    /// <param name="builder">The kernel builder.</param>
    /// <param name="authorizationKey">The GigaChat authorization key (stored in user-secrets with key "GigaChat:Token").</param>
    /// <param name="modelId">The model ID to use (default: "GigaChat").</param>
    /// <param name="endpoint">Optional API endpoint (default: https://gigachat.devices.sberbank.ru/api/v1).</param>
    /// <param name="httpClient">Optional HTTP client.</param>
    /// <param name="serviceId">Optional service ID for keyed services.</param>
    /// <returns>The kernel builder.</returns>
    public static IKernelBuilder AddGigaChatChatCompletion(
        this IKernelBuilder builder,
        string authorizationKey,
        string modelId = "GigaChat",
        string? endpoint = null,
        HttpClient? httpClient = null,
        string? serviceId = null)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            builder.Services.AddSingleton<IChatCompletionService>(sp => 
                new GigaChatChatCompletionService(authorizationKey, modelId, endpoint, httpClient));
        }
        else
        {
            builder.Services.AddKeyedSingleton<IChatCompletionService>(serviceId, (sp, key) => 
                new GigaChatChatCompletionService(authorizationKey, modelId, endpoint, httpClient));
        }
        
        return builder;
    }

    /// <summary>
    /// Adds GigaChat text generation service to the kernel.
    /// </summary>
    /// <param name="builder">The kernel builder.</param>
    /// <param name="authorizationKey">The GigaChat authorization key.</param>
    /// <param name="modelId">The model ID to use (default: "GigaChat").</param>
    /// <param name="endpoint">Optional API endpoint.</param>
    /// <param name="httpClient">Optional HTTP client.</param>
    /// <param name="serviceId">Optional service ID for keyed services.</param>
    /// <returns>The kernel builder.</returns>
    public static IKernelBuilder AddGigaChatTextGeneration(
        this IKernelBuilder builder,
        string authorizationKey,
        string modelId = "GigaChat",
        string? endpoint = null,
        HttpClient? httpClient = null,
        string? serviceId = null)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            builder.Services.AddSingleton<ITextGenerationService>(sp => 
                new GigaChatTextGenerationService(authorizationKey, modelId, endpoint, httpClient));
        }
        else
        {
            builder.Services.AddKeyedSingleton<ITextGenerationService>(serviceId, (sp, key) => 
                new GigaChatTextGenerationService(authorizationKey, modelId, endpoint, httpClient));
        }
        
        return builder;
    }

    /// <summary>
    /// Adds GigaChat text embedding generation service to the kernel.
    /// This method is deprecated. Use Microsoft.Extensions.AI.IEmbeddingGenerator instead.
    /// </summary>
    /// <param name="builder">The kernel builder.</param>
    /// <param name="authorizationKey">The GigaChat authorization key.</param>
    /// <param name="modelId">The model ID to use (default: "Embeddings"). Available: "Embeddings", "EmbeddingsGigaR".</param>
    /// <param name="endpoint">Optional API endpoint.</param>
    /// <param name="httpClient">Optional HTTP client.</param>
    /// <param name="serviceId">Optional service ID for keyed services.</param>
    /// <returns>The kernel builder.</returns>
#pragma warning disable SKEXP0001 // ITextEmbeddingGenerationService is experimental
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete("Use Microsoft.Extensions.AI.IEmbeddingGenerator<string, Embedding<float>> instead. This will be removed in a future version.")]
    public static IKernelBuilder AddGigaChatTextEmbeddingGeneration(
        this IKernelBuilder builder,
        string authorizationKey,
        string modelId = "Embeddings",
        string? endpoint = null,
        HttpClient? httpClient = null,
        string? serviceId = null)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            builder.Services.AddSingleton<ITextEmbeddingGenerationService>(sp => 
                new GigaChatTextEmbeddingGenerationService(authorizationKey, modelId, endpoint, httpClient));
        }
        else
        {
            builder.Services.AddKeyedSingleton<ITextEmbeddingGenerationService>(serviceId, (sp, key) => 
                new GigaChatTextEmbeddingGenerationService(authorizationKey, modelId, endpoint, httpClient));
        }
        
        return builder;
    }
#pragma warning restore CS0618
#pragma warning restore SKEXP0001

    /// <summary>
    /// Adds both GigaChat chat completion and text generation services to the kernel.
    /// </summary>
    /// <param name="builder">The kernel builder.</param>
    /// <param name="authorizationKey">The GigaChat authorization key.</param>
    /// <param name="modelId">The model ID to use (default: "GigaChat").</param>
    /// <param name="endpoint">Optional API endpoint.</param>
    /// <param name="httpClient">Optional HTTP client.</param>
    /// <param name="serviceId">Optional service ID for keyed services.</param>
    /// <returns>The kernel builder.</returns>
    public static IKernelBuilder AddGigaChat(
        this IKernelBuilder builder,
        string authorizationKey,
        string modelId = "GigaChat",
        string? endpoint = null,
        HttpClient? httpClient = null,
        string? serviceId = null)
    {
        builder.AddGigaChatChatCompletion(authorizationKey, modelId, endpoint, httpClient, serviceId);
        builder.AddGigaChatTextGeneration(authorizationKey, modelId, endpoint, httpClient, serviceId);
        
        return builder;
    }
}
