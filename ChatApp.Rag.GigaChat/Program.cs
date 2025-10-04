using Microsoft.Extensions.AI;
using ChatApp.Rag.GigaChat.Components;
using ChatApp.Rag.GigaChat.Services;
using ChatApp.Rag.GigaChat.Services.Ingestion;
using GigaChat.SemanticKernel;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Configure GigaChat for chat completions
// You will need to set the GigaChat token in user secrets:
//   cd this-project-directory
//   dotnet user-secrets set GigaChat:Token YOUR-GIGACHAT-TOKEN
var gigaChatToken = builder.Configuration["GigaChat:Token"] 
    ?? throw new InvalidOperationException("Missing configuration: GigaChat:Token. Please set it using 'dotnet user-secrets set GigaChat:Token YOUR-TOKEN'");

// Build Semantic Kernel with GigaChat
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddGigaChatChatCompletion(
    authorizationKey: gigaChatToken,
    modelId: "GigaChat"  // Available models: GigaChat, GigaChat-Plus, GigaChat-Pro
);
var kernel = kernelBuilder.Build();

// Get the chat completion service and wrap it for Microsoft.Extensions.AI
var gigaChatService = kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
var chatClient = new GigaChatAIChatClient(gigaChatService, "GigaChat");

// Use GigaChat embeddings instead of OpenAI
// Available models: "Embeddings" (default), "EmbeddingsGigaR" (advanced with larger context)
var embeddingGenerator = new GigaChatEmbeddingGenerator(gigaChatToken, "Embeddings");

var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "vector-store.db");
var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
builder.Services.AddSqliteCollection<string, IngestedChunk>("data-chatapp_rag_gigachat-chunks", vectorStoreConnectionString);
builder.Services.AddSqliteCollection<string, IngestedDocument>("data-chatapp_rag_gigachat-documents", vectorStoreConnectionString);

builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();
builder.Services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();
builder.Services.AddEmbeddingGenerator(embeddingGenerator);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// By default, we ingest PDF files from the /wwwroot/Data directory. You can ingest from
// other sources by implementing IIngestionSource.
// Important: ensure that any content you ingest is trusted, as it may be reflected back
// to users or could be a source of prompt injection risk.
await DataIngestor.IngestDataAsync(
    app.Services,
    new PDFDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data")));

app.Run();
