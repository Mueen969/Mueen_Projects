using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace EnterpriseAI.Core;

public class DiagnosticsPlugin
{
    [KernelFunction, Description("Gets the live server health status and CPU/Memory usage for a given environment.")]
    public string GetServerMetrics(
        [Description("The target deployment environment, e.g., 'Production', 'Staging', or 'Dev'")] string environment)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n⚡ [C# EXECUTION] Executing DiagnosticsPlugin.GetServerMetrics for '{environment}'...");
        Console.ResetColor();

        // Simulated enterprise system check logic
        if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            return "Environment: Production | CPU Usage: 89% | Memory Usage: 92% | Status: CRITICAL_HIGH_LOAD | Active Nodes: 4/4";
        }

        return $"Environment: {environment} | CPU Usage: 12% | Memory Usage: 34% | Status: HEALTHY | Active Nodes: 2/2";
    }

    [KernelFunction, Description("Restarts a specific microservice in a given environment to clear out-of-memory states.")]
    public string RestartService(
        [Description("The name of the microservice, e.g., 'OrderProcessor' or 'AuthService'")] string serviceName,
        [Description("The target environment, e.g., 'Production'")] string environment)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n⚡ [C# EXECUTION] Executing DiagnosticsPlugin.RestartService for '{serviceName}' in '{environment}'...");
        Console.ResetColor();

        return $"SUCCESS: Service '{serviceName}' in '{environment}' was gracefully restarted at {DateTime.UtcNow:HH:mm:ss} UTC.";
    }
}