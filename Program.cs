using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace EnterpriseAI.Core;

// 1. Define the exact structure we want back from the AI
public record TicketAnalysis(
    string SystemComponent,
    string Severity, // Low, Medium, High, Critical
    string RootCauseSummary,
    string SuggestedFix,
    string[] ErrorCodes
);

class Program_old
{
    static async Task Main_old(string[] args)
    {
        // 2. Initialize the Kernel with OpenAI Chat Completion
        // Note: You can easily swap this out for AddAzureOpenAIChatCompletion
        var builder = Kernel.CreateBuilder();

        //builder.AddOpenAIChatCompletion(
        //    modelId: "gpt-4o", // Using a solid frontier model
        //    apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        //            ?? throw new InvalidOperationException("Please set your OPENAI_API_KEY environment variable.")
        //);
        // 🛠️ SWAPPED: Pointing directly to your local machine running Ollama
        builder.AddOllamaChatCompletion(
            modelId: "llama3.2",
            endpoint: new Uri("http://localhost:11434")
        );

        Kernel kernel = builder.Build();
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        // 3. Mock data representing a chaotic production issue submitted by a user
        string userTicket = "The checkout system is completely broken. Customers are getting spinning wheels when hitting 'Pay Now'.";
        string logSnippet = "2026-07-08 12:04:11 [ERROR] OrderProcessor: Payment gateway timeout on endpoint /v3/charge. HTTP 504. System.TimeoutException at Company.Payments.Gateway.Submit(...)";

        // 4. Crafting the System Prompt with structured layout and strict constraints
        var history = new ChatHistory();
        history.AddSystemMessage(
            "You are an expert Enterprise Site Reliability Engineer (SRE).\n" +
            "Your task is to analyze support tickets alongside log snippets and output a strictly formatted JSON object.\n" +
            "You must strictly follow the JSON schema provided. Do not include markdown formatting like ```json or trailing text."
        );

        // 5. Few-Shot Prompting: Providing an explicit example of input -> expected output
        history.AddUserMessage(
            "Ticket: Login page fails for European users.\n" +
            "Log: 2026-07-08 [WARN] AuthDB: Connection throttled for region EU-West. SQL Error 1205."
        );

        var exampleOutput = new TicketAnalysis(
            SystemComponent: "Authentication Database",
            Severity: "High",
            RootCauseSummary: "Database connections are being throttled in the EU-West region.",
            SuggestedFix: "Check active connection pool sizes and scaling rules for the EU database replica.",
            ErrorCodes: new[] { "SQL-1205" }
        );
        history.AddAssistantMessage(JsonSerializer.Serialize(exampleOutput));

        // 6. Injecting the actual problem we want solved
        history.AddUserMessage($"Ticket: {userTicket}\nLog Snippet: {logSnippet}");

        Console.WriteLine("Analyzing infrastructure ticket...");

        // 7. Execute the request
        var response = await chatService.GetChatMessageContentAsync(history);
        string rawJson = response.ToString().Trim();

        // 8. Safely parse the result straight into our domain model
        try
        {
            var analysis = JsonSerializer.Deserialize<TicketAnalysis>(rawJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Console.WriteLine("\n--- Analysis Successfully Parsed ---");
            Console.WriteLine($"Component:  {analysis?.SystemComponent}");
            Console.WriteLine($"Severity:   {analysis?.Severity}");
            Console.WriteLine($"Summary:    {analysis?.RootCauseSummary}");
            Console.WriteLine($"Fix:        {analysis?.SuggestedFix}");
            Console.WriteLine($"Codes:      {string.Join(", ", analysis?.ErrorCodes ?? Array.Empty<string>())}");
        }
        catch (JsonException ex)
        {
            Console.WriteLine("\n❌ Failed to parse output into a structured C# object.");
            Console.WriteLine($"Raw AI Output: {rawJson}");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}


class Program
{
    static async Task Main(string[] args)
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOllamaChatCompletion(modelId: "llama3.2", endpoint: new Uri("http://localhost:11434"));
        Kernel kernel = builder.Build();
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        history.AddSystemMessage("Analyze the logs and provide structural summaries.");
        history.AddUserMessage("Ticket: [2026-06-03 06:08:12,138] ERROR: Nybl.Module.VisionAI.SEC.Inspections.Common.Services.ImageAnalysisService >> System.Data.Entity.Infrastructure.DbUpdateException: An error occurred while updating the entries. See the inner exception for details. ---> System.Data.Entity.Core.UpdateException: An error occurred while updating the entries. See the inner exception for details. ---> System.Data.SqlClient.SqlException: The INSERT statement conflicted with the FOREIGN KEY constraint \"FK_N_Defects_N_DefectSeverity\". The conflict occurred in database \"NYBL_VisionAI_SEC_EXTENDED\", table \"dbo.N_DefectSeverity\", column 'DSV_ID'.\r\nThe statement has been terminated.\r\n   at System.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)\r\n   at System.Data.SqlClient.TdsParser.ThrowExceptionAndWarning(TdsParserStateObject stateObj, Boolean callerHasConnectionLock, Boolean asyncClose)\r\n   at System.Data.SqlClient.TdsParser.TryRun(RunBehavior runBehavior, SqlCommand cmdHandler, SqlDataReader dataStream, BulkCopySimpleResultSet bulkCopyHandler, TdsParserStateObject stateObj, Boolean& dataReady)\r\n   at System.Data.SqlClient.SqlDataReader.TryConsumeMetaData()\r\n   at System.Data.SqlClient.SqlDataReader.get_MetaData()\r\n   at System.Data.SqlClient.SqlCommand.FinishExecuteReader(SqlDataReader ds, RunBehavior runBehavior, String resetOptionsString, Boolean isInternal, Boolean forDescribeParameterEncryption, Boolean shouldCacheForAlwaysEncrypted)\r\n   at System.Data.SqlClient.SqlCommand.RunExecuteReaderTds(CommandBehavior cmdBehavior, RunBehavior runBehavior, Boolean returnStream, Boolean async, Int32 timeout, Task& task, Boolean asyncWrite, Boolean inRetry, SqlDataReader ds, Boolean describeParameterEncryptionRequest)\r\n   at System.Data.SqlClient.SqlCommand.RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, Boolean returnStream, String method, TaskCompletionSource`1 completion, Int32 timeout, Task& task, Boolean& usedCache, Boolean asyncWrite, Boolean inRetry)\r\n   at System.Data.SqlClient.SqlCommand.RunExecuteReader(CommandBehavior cmdBehavior, RunBehavior runBehavior, Boolean returnStream, String method)\r\n   at System.Data.SqlClient.SqlCommand.ExecuteReader(CommandBehavior behavior, String method)\r\n   at System.Data.Entity.Infrastructure.Interception.InternalDispatcher`1.Dispatch[TTarget,TInterceptionContext,TResult](TTarget target, Func`3 operation, TInterceptionContext interceptionContext, Action`3 executing, Action`3 executed)\r\n   at System.Data.Entity.Infrastructure.Interception.DbCommandDispatcher.Reader(DbCommand command, DbCommandInterceptionContext interceptionContext)\r\n   at System.Data.Entity.Core.Mapping.Update.Internal.DynamicUpdateCommand.Execute(Dictionary`2 identifierValues, List`1 generatedValues)\r\n   at System.Data.Entity.Core.Mapping.Update.Internal.UpdateTranslator.Update()\r\n   --- End of inner exception stack trace ---\r\n   at System.Data.Entity.Core.Mapping.Update.Internal.UpdateTranslator.Update()\r\n   at System.Data.Entity.Core.Objects.ObjectContext.ExecuteInTransaction[T](Func`1 func, IDbExecutionStrategy executionStrategy, Boolean startLocalTransaction, Boolean releaseConnectionOnSuccess)\r\n   at System.Data.Entity.Core.Objects.ObjectContext.SaveChangesToStore(SaveOptions options, IDbExecutionStrategy executionStrategy, Boolean startLocalTransaction)\r\n   at System.Data.Entity.Infrastructure.DbExecutionStrategy.Execute[TResult](Func`1 operation)\r\n   at Enso.EntityFramework.SqlDbExecutionStrategy.Execute[TResult](Func`1 operation) in D:\\Nybl\\Projects\\NyblCore\\Sole\\Source\\Development\\DataModels\\Enso.EntityFramework\\SqlDbExecutionStrategy.cs:line 78\r\n   at System.Data.Entity.Core.Objects.ObjectContext.SaveChangesInternal(SaveOptions options, Boolean executeInExistingTransaction)\r\n   at System.Data.Entity.Internal.InternalContext.SaveChanges()\r\n   --- End of inner exception stack trace ---\r\n   at System.Data.Entity.Internal.InternalContext.SaveChanges()\r\n   at Nybl.Module.VisionAI.SEC.Inspections.Common.Services.ImageAnalysisService.InsertBulk(List`1 imageAnalysis) in D:\\Nybl\\Projects\\NyblCore\\Sole\\Source\\Development\\VisionAI\\Modules\\Nybl.Module.VisionAI.SEC.Inspections\\Nybl.Module.VisionAI.SEC.Inspections.Common\\Services\\ImageAnalysisService.cs:line 39\r\n");

        // 🛠️ The Magic Part: Tell the execution layer to strictly enforce JSON
        // We use System.Text.Json to reflect your C# record into a valid JSON Schema dynamically
        var executionSettings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>
            {
                { "response_format", new { type = "json_object" } }
            }
        };

        var response = await chatService.GetChatMessageContentAsync(
            history,
            executionSettings: executionSettings // <-- Enforcing schema at engine level
        );

        Console.WriteLine(response.ToString());
    }
}