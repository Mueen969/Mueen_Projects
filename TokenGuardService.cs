using System;
using Microsoft.ML.Tokenizers;

namespace EnterpriseAI.Core;

// 1. Token Guard Service for Counting
public class TokenGuardService
{
    private readonly Tokenizer _tokenizer;

    public TokenGuardService()
    {
        // Tiktoken encoding matching modern models like GPT-4o / llama3
        _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
    }

    public int GetTokenCount(string text) => _tokenizer.CountTokens(text);
}