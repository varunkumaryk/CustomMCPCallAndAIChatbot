using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- MCP Client (Singleton) ---
// Connects once at startup to the Azure-hosted MCP server (streamable HTTP transport).
// SK auto-discovers GetWeatherForecast and GetTest2Key from it.
var mcpTransport = new HttpClientTransport(new HttpClientTransportOptions
{
    Endpoint = new Uri(builder.Configuration["McpServer:Url"]!),
    TransportMode = HttpTransportMode.StreamableHttp,
    Name = "WeatherForecastMCP"
});
var mcpClient = await McpClient.CreateAsync(mcpTransport, new McpClientOptions
{
    ClientInfo = new() { Name = "AIChatbot", Version = "1.0.0" }
});
builder.Services.AddSingleton(mcpClient);

// --- Semantic Kernel (Transient) ---
// Fresh kernel per request so each request gets a clean plugin list.
builder.Services.AddTransient<Kernel>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    return Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(
            deploymentName: cfg["AzureOpenAI:DeploymentName"]!,
            endpoint: cfg["AzureOpenAI:Endpoint"]!,
            apiKey: cfg["AzureOpenAI:ApiKey"]!)
        .Build();
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
