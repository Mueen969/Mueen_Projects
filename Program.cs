using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace EnterpriseAI.Core;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Kernel.CreateBuilder();

        // Connect to your local Ollama instance
        builder.AddOllamaChatCompletion(
            modelId: "llama3.2",
            endpoint: new Uri("http://localhost:11434")
        );

        Kernel kernel = builder.Build();
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        history.AddSystemMessage("You are an elite, concise senior developer assistant.");

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("System: Local Ollama Streaming initialized. Type 'exit' to quit.\n");
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

            var fullResponse = new StringBuilder();

            // 🛠️ The magic is here: Iterating asynchronously over the live stream of token chunks
            IAsyncEnumerable<StreamingChatMessageContent> streamingStream =
                chatService.GetStreamingChatMessageContentsAsync(history, kernel: kernel);

            await foreach (StreamingChatMessageContent chunk in streamingStream)
            {
                // Note: Some chunks only contain metadata (like tool-calls). 
                // We must check if the text content is not null before writing.
                if (chunk.Content is not null)
                {
                    Console.Write(chunk.Content);
                    fullResponse.Append(chunk.Content);
                }
            }

            Console.WriteLine("\n"); // Clear line for next turn

            // Critical Step: Save the combined stream output back into history so the next turn remembers it!
            history.AddAssistantMessage(fullResponse.ToString());
        }
    }
}