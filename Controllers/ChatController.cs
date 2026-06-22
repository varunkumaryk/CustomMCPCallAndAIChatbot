using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using ModelContextProtocol.Client;

namespace CustomMCPCallAndAIChatbot.Controllers;

[ApiController]
[Route("[controller]")]
public class ChatController(Kernel kernel, McpClient mcpClient) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request, CancellationToken ct)
    {
        // Auto-discover all tools from the MCP server and register them with SK.
        // When the LLM decides it needs weather data, SK invokes the MCP tool automatically.
        var mcpTools = await mcpClient.ListToolsAsync(cancellationToken: ct);
        kernel.Plugins.AddFromFunctions("WeatherForecastMCP",
            mcpTools.Select(t => t.AsKernelFunction()));

#pragma warning disable SKEXP0001
        var settings = new AzureOpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
#pragma warning restore SKEXP0001

        var result = await kernel.InvokePromptAsync(
            request.Message,
            new KernelArguments(settings),
            cancellationToken: ct);

        return Ok(new ChatResponse(result.ToString()));
    }
}

public record ChatRequest(string Message);
public record ChatResponse(string Response);
