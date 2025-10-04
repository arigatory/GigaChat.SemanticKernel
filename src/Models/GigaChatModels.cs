using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

#nullable enable

namespace GigaChat.SemanticKernel.Models;

public sealed class GigaChatTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }
}

public sealed class GigaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "GigaChat";

    [JsonPropertyName("messages")]
    public List<GigaChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public double? TopP { get; set; }

    [JsonPropertyName("n")]
    public int? N { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("repetition_penalty")]
    public double? RepetitionPenalty { get; set; }

    [JsonPropertyName("update_interval")]
    public int? UpdateInterval { get; set; }
}

public sealed class GigaChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public sealed class GigaChatResponse
{
    [JsonPropertyName("choices")]
    public List<GigaChatChoice> Choices { get; set; } = new();

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("usage")]
    public GigaChatUsage? Usage { get; set; }

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;
}

public sealed class GigaChatChoice
{
    [JsonPropertyName("message")]
    public GigaChatMessage? Message { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public sealed class GigaChatUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public sealed class GigaChatStreamResponse
{
    [JsonPropertyName("choices")]
    public List<GigaChatStreamChoice> Choices { get; set; } = new();

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;
}

public sealed class GigaChatStreamChoice
{
    [JsonPropertyName("delta")]
    public GigaChatDelta? Delta { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

public sealed class GigaChatDelta
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

// Embeddings models
public sealed class GigaChatEmbeddingsRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "Embeddings";

    [JsonPropertyName("input")]
    public List<string> Input { get; set; } = new();
}

public sealed class GigaChatEmbeddingsResponse
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<GigaChatEmbeddingData> Data { get; set; } = new();

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
}

public sealed class GigaChatEmbeddingData
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = "embedding";

    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = Array.Empty<float>();

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("usage")]
    public GigaChatEmbeddingUsage? Usage { get; set; }
}

public sealed class GigaChatEmbeddingUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
}
