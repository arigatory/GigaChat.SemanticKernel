using Microsoft.Extensions.AI;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AITextContent = Microsoft.Extensions.AI.TextContent;

namespace ChatApp.Rag.GigaChat.Services;

/// <summary>
/// GigaChat client with Function Calling support via Microsoft.Extensions.AI
/// </summary>
public sealed class GigaChatAIChatClient : IChatClient
{
    private readonly string _authData;
    private readonly string _modelId;
    private readonly ILogger<GigaChatAIChatClient> _logger;
    private string? _accessToken;
    private DateTime _tokenExpiration;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public GigaChatAIChatClient(string authData, string modelId, ILogger<GigaChatAIChatClient> logger)
    {
        _authData = authData;
        _modelId = modelId;
        _logger = logger;
    }

    public ChatClientMetadata Metadata => new("GigaChat");

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await GetResponseCoreAsync(chatMessages, options, cancellationToken);
    }

    private async Task<ChatResponse> GetResponseCoreAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var conversation = chatMessages.ToList();
        
        // Convert AITool to AIFunction
        List<AIFunction>? tools = null;
        if (options?.Tools != null)
        {
            tools = new List<AIFunction>();
            foreach (var tool in options.Tools)
            {
                if (tool is AIFunction func)
                {
                    tools.Add(func);
                }
                else
                {
                    // Try to extract function from tool
                    var funcProp = tool.GetType().GetProperty("AIFunction");
                    if (funcProp?.GetValue(tool) is AIFunction extractedFunc)
                    {
                        tools.Add(extractedFunc);
                    }
                }
            }
        }

        var functionCallCount = 0;
        const int maxFunctionCalls = 5;

        while (true)
        {
            var request = BuildGigaChatRequest(conversation, options, tools);
            var response = await SendRequestAsync(request, cancellationToken);

            if (response.Choices == null || response.Choices.Length == 0)
            {
                throw new InvalidOperationException("No response from GigaChat");
            }

            var choice = response.Choices[0];

            // Check if we need to call a function
            if (choice.Message.FunctionCall != null && tools?.Any() == true && functionCallCount < maxFunctionCalls)
            {
                var functionCall = choice.Message.FunctionCall;
                var functionName = functionCall.Name;
                var functionArgs = functionCall.GetArgumentsAsString();

                _logger.LogInformation("Function call requested: {functionName} with args: {args}", functionName, functionArgs);

                // Find the matching tool
                var tool = tools.FirstOrDefault(t => t.Name == functionName);
                if (tool == null)
                {
                    _logger.LogWarning("Function {functionName} not found in tools", functionName);
                    // Return response without function call if tool not found
                    var responseContent = choice.Message.Content ?? string.Empty;
                    return new ChatResponse([
                        new ChatMessage(ChatRole.Assistant, [new AITextContent(responseContent)])
                    ])
                    {
                        ModelId = _modelId
                    };
                }

                // Execute the function
                var functionResult = await CallFunctionAsync(tool, functionArgs, cancellationToken);
                _logger.LogInformation("Function {functionName} returned: {result}", functionName, functionResult);

                // Add assistant message with function call to conversation
                var assistantMessage = new ChatMessage(ChatRole.Assistant, string.Empty);
                assistantMessage.AdditionalProperties ??= new AdditionalPropertiesDictionary();
                assistantMessage.AdditionalProperties["function_call"] = new Dictionary<string, string>
                {
                    ["name"] = functionName,
                    ["arguments"] = functionArgs
                };
                conversation.Add(assistantMessage);

                // Add function result to conversation
                var functionMessage = new ChatMessage(ChatRole.Assistant, functionResult);
                functionMessage.AdditionalProperties ??= new AdditionalPropertiesDictionary();
                functionMessage.AdditionalProperties["role"] = "function";
                functionMessage.AdditionalProperties["name"] = functionName;
                conversation.Add(functionMessage);

                functionCallCount++;
                continue; // Loop back to get the next response
            }

            // No more function calls needed, return the response
            var content = choice.Message.Content ?? string.Empty;
            return new ChatResponse([
                new ChatMessage(ChatRole.Assistant, [new AITextContent(content)])
            ])
            {
                ModelId = _modelId,
                Usage = response.Usage != null ? new()
                {
                    InputTokenCount = response.Usage.PromptTokens,
                    OutputTokenCount = response.Usage.CompletionTokens,
                    TotalTokenCount = response.Usage.TotalTokens
                } : null
            };
        }
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // For streaming, we first get the full response (with function calling if needed)
        var response = await GetResponseCoreAsync(chatMessages, options, cancellationToken);
        var text = response.Messages[0].Text ?? "";

        // Then simulate streaming by breaking text into chunks
        const int chunkSize = 5;
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            var chunk = text.Substring(i, Math.Min(chunkSize, text.Length - i));
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [new AITextContent(chunk)]
            };
            await Task.Delay(50, cancellationToken);
        }
    }

    public void Dispose()
    {
        _tokenLock.Dispose();
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    private GigaChatRequest BuildGigaChatRequest(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options,
        List<AIFunction>? tools)
    {
        var request = new GigaChatRequest
        {
            Model = _modelId,
            Messages = ConvertToGigaChatMessages(chatMessages),
            Temperature = (float)(options?.Temperature ?? 0.7),
            MaxTokens = options?.MaxOutputTokens ?? 1024
        };

        if (tools?.Any() == true)
        {
            request.Functions = GenerateGigaChatFunctions(tools);
            request.FunctionCall = "auto";
        }

        return request;
    }

    private static GigaChatMessage[] ConvertToGigaChatMessages(IEnumerable<ChatMessage> chatMessages)
    {
        var result = new List<GigaChatMessage>();

        foreach (var message in chatMessages)
        {
            var gigaChatMessage = new GigaChatMessage
            {
                Role = message.Role.Value,
                Content = string.Join("", message.Contents.OfType<AITextContent>().Select(c => c.Text))
            };

            // Handle function call and function result messages
            if (message.AdditionalProperties != null)
            {
                if (message.AdditionalProperties.TryGetValue("role", out var roleObj) && roleObj?.ToString() == "function")
                {
                    gigaChatMessage.Role = "function";
                    if (message.AdditionalProperties.TryGetValue("name", out var nameObj))
                    {
                        gigaChatMessage.Name = nameObj?.ToString();
                    }
                }
                else if (message.AdditionalProperties.TryGetValue("function_call", out var functionCallObj))
                {
                    if (functionCallObj is Dictionary<string, string> fc)
                    {
                        gigaChatMessage.FunctionCall = new GigaChatFunctionCall
                        {
                            Name = fc.GetValueOrDefault("name", ""),
                            Arguments = JsonDocument.Parse(fc.GetValueOrDefault("arguments", "{}")).RootElement
                        };
                    }
                }
            }

            result.Add(gigaChatMessage);
        }

        return result.ToArray();
    }

    private static GigaChatFunction[] GenerateGigaChatFunctions(List<AIFunction> tools)
    {
        return tools.Select(tool =>
        {
            var parameters = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>(),
                ["required"] = new List<string>()
            };

            var properties = (Dictionary<string, object>)parameters["properties"];
            var required = (List<string>)parameters["required"];

            // Try to extract parameter info from the function's metadata
            var methodInfo = tool.UnderlyingMethod;
            if (methodInfo != null)
            {
                var methodParams = methodInfo.GetParameters();
                foreach (var param in methodParams)
                {
                    var paramName = param.Name ?? "param";
                    var paramSchema = new Dictionary<string, object>
                    {
                        ["type"] = GetJsonSchemaType(param.ParameterType)
                    };

                    // Try to get description from attributes
                    var descAttr = param.GetCustomAttributes(false)
                        .FirstOrDefault(attr => attr.GetType().Name.Contains("Description"));
                    if (descAttr != null)
                    {
                        var descProp = descAttr.GetType().GetProperty("Description");
                        if (descProp?.GetValue(descAttr) is string desc)
                        {
                            paramSchema["description"] = desc;
                        }
                    }

                    properties[paramName] = paramSchema;

                    if (!param.HasDefaultValue)
                    {
                        required.Add(paramName);
                    }
                }
            }

            return new GigaChatFunction
            {
                Name = tool.Name,
                Description = tool.Description,
                Parameters = parameters
            };
        }).ToArray();
    }

    private static string GetJsonSchemaType(Type type)
    {
        if (type == typeof(string) || type == typeof(char))
            return "string";
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
            type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort))
            return "integer";
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return "number";
        if (type == typeof(bool))
            return "boolean";
        if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
            return "array";

        return "string";
    }

    private async Task<string> CallFunctionAsync(AIFunction tool, string arguments, CancellationToken cancellationToken)
    {
        try
        {
            var functionArgs = ParseFunctionArguments(arguments);
            var result = await tool.InvokeAsync(functionArgs, cancellationToken);
            return result?.ToString() ?? "Function executed successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {functionName}", tool.Name);
            return $"Error executing function: {ex.Message}";
        }
    }

    private static AIFunctionArguments ParseFunctionArguments(string arguments)
    {
        var functionArgs = new AIFunctionArguments();

        try
        {
            if (arguments.Trim().StartsWith('{') && arguments.Trim().EndsWith('}'))
            {
                var jsonDoc = JsonDocument.Parse(arguments);
                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    object? value = ConvertJsonValueToObject(property.Value);
                    functionArgs[property.Name] = value;
                }
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse function arguments: {arguments}", ex);
        }

        return functionArgs;
    }

    private static object? ConvertJsonValueToObject(JsonElement jsonElement)
    {
        return jsonElement.ValueKind switch
        {
            JsonValueKind.String => jsonElement.GetString(),
            JsonValueKind.Number when jsonElement.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.Number when jsonElement.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.Number => jsonElement.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => jsonElement.EnumerateArray().Select(ConvertJsonValueToObject).ToArray(),
            JsonValueKind.Object => jsonElement.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonValueToObject(p.Value)),
            _ => jsonElement.ToString()
        };
    }

    private async Task<GigaChatResponse> SendRequestAsync(GigaChatRequest request, CancellationToken cancellationToken)
    {
        await EnsureTokenAsync(cancellationToken);

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        using var httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "GigaChatClient/1.0");

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(
            "https://gigachat.devices.sberbank.ru/api/v1/chat/completions",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<GigaChatResponse>(responseJson)
            ?? throw new InvalidOperationException("Failed to deserialize GigaChat response");
    }

    private async Task EnsureTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiration)
        {
            return;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiration)
            {
                return;
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            using var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _authData);
            httpClient.DefaultRequestHeaders.Add("RqUID", Guid.NewGuid().ToString());
            httpClient.DefaultRequestHeaders.Add("User-Agent", "GigaChatClient/1.0");

            var formParams = new List<KeyValuePair<string, string>>
            {
                new("scope", "GIGACHAT_API_PERS")
            };

            var content = new FormUrlEncodedContent(formParams);
            var response = await httpClient.PostAsync(
                "https://ngw.devices.sberbank.ru:9443/api/v2/oauth",
                content,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<GigaChatTokenResponse>(responseJson);

            _accessToken = tokenResponse?.AccessToken
                ?? throw new InvalidOperationException("Failed to get access token");
            _tokenExpiration = DateTimeOffset.FromUnixTimeMilliseconds(tokenResponse.ExpiresAt).DateTime;

            _logger.LogInformation("Got new GigaChat access token, expires at {expiration}", _tokenExpiration);
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}

// GigaChat API models
public class GigaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "GigaChat";

    [JsonPropertyName("messages")]
    public GigaChatMessage[] Messages { get; set; } = [];

    [JsonPropertyName("temperature")]
    public float Temperature { get; set; } = 0.7f;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 1024;

    [JsonPropertyName("functions")]
    public GigaChatFunction[]? Functions { get; set; }

    [JsonPropertyName("function_call")]
    public object? FunctionCall { get; set; }
}

public class GigaChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("function_call")]
    public GigaChatFunctionCall? FunctionCall { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class GigaChatResponse
{
    [JsonPropertyName("choices")]
    public GigaChatChoice[]? Choices { get; set; }

    [JsonPropertyName("usage")]
    public GigaChatUsage? Usage { get; set; }
}

public class GigaChatChoice
{
    [JsonPropertyName("message")]
    public GigaChatMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = "";
}

public class GigaChatUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int? PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int? CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int? TotalTokens { get; set; }
}

public class GigaChatTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }
}

public class GigaChatFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class GigaChatFunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("arguments")]
    public JsonElement Arguments { get; set; }

    public string GetArgumentsAsString()
    {
        return Arguments.ValueKind == JsonValueKind.String
            ? Arguments.GetString() ?? ""
            : Arguments.ToString();
    }
}
