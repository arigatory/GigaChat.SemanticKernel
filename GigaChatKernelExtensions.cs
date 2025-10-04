using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.TextGeneration;

namespace GigaChat.SemanticKernel;

public static class GigaChatKernelExtensions
{
    public static IKernelBuilder AddGigaChat(
        this IKernelBuilder builder,
        string apiKey,
        string modelId = "GigaChat-Pro",
        string? endpoint = null,
        HttpClient? httpClient = null)
    {
        builder.Services.AddKeyedSingleton<IChatCompletionService>(modelId, (_, _) => 
            new GigaChatChatCompletionService(apiKey, modelId, endpoint, httpClient));
        
        builder.Services.AddKeyedSingleton<ITextGenerationService>(modelId, (_, _) => 
            new GigaChatTextGenerationService(apiKey, modelId, endpoint, httpClient));
        
        // Add other services similarly
        return builder;
    }
}
