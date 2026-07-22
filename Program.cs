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
        builder.AddOllamaChatCompletion(
            modelId: "llama3.2",
            endpoint: new Uri("http://localhost:11434")
        );

        Kernel kernel = builder.Build();
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        // Initialize our token management middleware
        var tokenGuard = new TokenGuardService();
        var budgetManager = new HistoryBudgetManager(tokenGuard, maxTokenBudget: 150); // Tight budget to test purging

        var history = new ChatHistory();
        history.AddSystemMessage("You are an assistant. Answer in 1 short sentence.");

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("System: Token-Managed Conversation Loop Active.");
        Console.WriteLine("Token budget set to ~150 tokens. Watch old messages drop when limit is hit!\n");
        Console.ResetColor();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("You: ");
            Console.ResetColor();

            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            // Step A: Append User Input
            history.AddUserMessage(input);

            // 🛠️ Step B: Middleware - ENFORCE BUDGET BEFORE MAKING THE API CALL
            budgetManager.EnforceTokenBudget(history);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("AI: ");
            Console.ResetColor();

            // Step C: Send request over the wire with the guaranteed lean payload
            var response = await chatService.GetChatMessageContentAsync(history);
            Console.WriteLine(response.Content);

            // Step D: Append AI Output to memory
            history.AddAssistantMessage(response.Content ?? string.Empty);
            Console.WriteLine();
        }
    }
}