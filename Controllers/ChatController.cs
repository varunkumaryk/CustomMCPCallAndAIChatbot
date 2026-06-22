using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using ModelContextProtocol.Client;

namespace CustomMCPCallAndAIChatbot.Controllers;

[ApiController]
[Route("[controller]")]
public class ChatController(Kernel kernel, McpClient mcpClient) : ControllerBase
{
    private const string SystemPrompt =
        "You are a focused assistant that can ONLY answer questions by calling the tools available to you. " +
        "Do NOT use your own training knowledge to answer any question. " +
        "If the user's question cannot be answered by one of your available tools, respond with: " +
        "'I can only answer questions that are supported by the available tools. " +
        "Please ask something I can help with using those tools.'";

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request, CancellationToken ct)
    {
        var mcpTools = await mcpClient.ListToolsAsync(cancellationToken: ct);
        kernel.Plugins.AddFromFunctions("WeatherForecastMCP",
            mcpTools.Select(t => t.AsKernelFunction()));

        var history = new ChatHistory(SystemPrompt);
        history.AddUserMessage(request.Message);

#pragma warning disable SKEXP0001
        var settings = new AzureOpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
#pragma warning restore SKEXP0001

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var result = await chatService.GetChatMessageContentAsync(history, settings, kernel, ct);

        return Ok(new ChatResponse(result.Content ?? string.Empty));
    }
}

public record ChatRequest(string Message);
public record ChatResponse(string Response);
