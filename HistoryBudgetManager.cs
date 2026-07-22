using EnterpriseAI.Core;
using Microsoft.SemanticKernel.ChatCompletion;
// 2. Memory Manager that actively trims ChatHistory
public class HistoryBudgetManager
{
    private readonly TokenGuardService _tokenGuard;
    private readonly int _maxTokenBudget;

    public HistoryBudgetManager(TokenGuardService tokenGuard, int maxTokenBudget = 300)
    {
        _tokenGuard = tokenGuard;
        // Setting a low budget (e.g., 300 tokens) for demonstration so you can see it purge old turns!
        _maxTokenBudget = maxTokenBudget;
    }

    public void EnforceTokenBudget(ChatHistory history)
    {
        // Don't purge if we only have System Prompt or zero history
        if (history.Count <= 2) return;

        int totalTokens = CalculateTotalTokens(history);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n[TOKEN METRICS] History currently contains {history.Count} messages (~{totalTokens} tokens).");
        Console.ResetColor();

        // If history exceeds our budget, drop the oldest user/assistant messages after the System Prompt
        while (totalTokens > _maxTokenBudget && history.Count > 2)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"⚠️ [BUDGET EXCEEDED] Memory ({totalTokens} tokens) > Limit ({_maxTokenBudget} tokens). Purging oldest turn...");
            Console.ResetColor();

            // Index 0 = System Prompt (Keep forever)
            // Index 1 = Oldest user/assistant message (Remove)
            history.RemoveAt(1);

            totalTokens = CalculateTotalTokens(history);
        }
    }

    private int CalculateTotalTokens(ChatHistory history)
    {
        int count = 0;
        foreach (var msg in history)
        {
            count += _tokenGuard.GetTokenCount(msg.Content ?? string.Empty);
        }
        return count;
    }
}