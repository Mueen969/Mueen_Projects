using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace EnterpriseAI.Core;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Kernel.CreateBuilder();

        // 1. Connect to Ollama (or OpenAI)
        builder.AddOllamaChatCompletion(
            modelId: "llama3.2",
            endpoint: new Uri("http://localhost:11434")
        );

        // 2. Register our C# Plugin into the Kernel container
        builder.Plugins.AddFromType<DiagnosticsPlugin>("Diagnostics");

        Kernel kernel = builder.Build();
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        // 3. Configure Execution Settings to allow automatic tool invocation
        var settings = new OllamaPromptExecutionSettings
        {
            // Tells SK: If the LLM requests a tool call, run the C# method automatically and feed the result back!
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var history = new ChatHistory();
        history.AddSystemMessage("You are an automated DevOps SRE assistant. Use available diagnostic tools to answer user issues.");

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("System: Local Function Calling initialized. Try asking:");
        Console.WriteLine(" -> 'Check the server metrics for Production'");
        Console.WriteLine(" -> 'Production CPU is spiking, please restart the OrderProcessor service'\n");
        Console.ResetColor();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("You: ");
            Console.ResetColor();

            string? userInput = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            history.AddUserMessage(userInput);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("AI: ");
            Console.ResetColor();

            // Execute request with auto-function calling enabled
            var response = await chatService.GetChatMessageContentAsync(
                history,
                executionSettings: settings,
                kernel: kernel
            );

            Console.WriteLine(response.Content);
            Console.WriteLine();

            history.AddAssistantMessage(response.Content ?? string.Empty);
        }
    }
}