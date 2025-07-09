using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using Kusto.Data;
using Kusto.Data.Net.Client;

//Main();
EchoTool.LogIntoKusto();
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";

    [McpServerTool, Description("Echoes the message length back to the client.")]
    public static string EchoLength(string message) => $"hello {message.Length}";

    [McpServerTool, Description("Gets a random English word")]
    public static string RandomWord()
    {
        var dict = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
        var randWord = dict.RandomWord();
        Console.Error.WriteLine($"From MyMCPServer Random word: {randWord}");
        // Don't use Console.WriteLine in MCP servers - it interferes with protocol communication
        // Use Console.Error.WriteLine for debugging instead, or remove entirely
        return randWord;
    }

    [McpServerTool, Description("Gets the words made from the letters of a word")]
    public static string GetSubWords(string word)
    {
        var dict = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
        var subWords = dict.GenerateSubWords(word);
        // convert to JSON
        var json = JsonSerializer.Serialize(subWords);
        Console.Error.WriteLine($"From MyMCPServer GetSubWords word: {word} subWords: {json}");
        // Don't use Console.WriteLine in MCP servers - it interferes with protocol communication
        // Use Console.Error.WriteLine for debugging instead, or remove entirely
        return json;
    }
    [McpServerTool, Description("Log into Kusto and query Fabric Telemetry for event names and counts")]
    public static string LogIntoKustoAndGetTelemetry()
    {
        var query = @"cluster('https://DDTelvscode.kusto.windows.net').database('VSCodeExt').RawEventsVSCodeExt
    | where ExtensionName == 'fabric.vscode-fabric' or  ExtensionName  == 'fabric.vscode-fabric-functions'
    | summarize count() by EventName
    | order  by count_ desc
";
        var result = queryKusto(query);
        return result;
    }

    [McpServerTool, Description("Gets common Fabric extension errors")]
    public static string GetFabricCommonErrors()
    {
        var query = """
            cluster('https://DDTelvscode.kusto.windows.net').database('VSCodeExt').RawEventsVSCodeExt
            | where ExtensionName == 'fabric.vscode-fabric' or ExtensionName == 'fabric.vscode-fabric-functions'
            | where EventName == 'fabric.vscode-fabric/extension/error' or EventName == 'fabric.vscode-fabric-functions/extension/error'
            | extend Fault = tostring(Properties['fault'])
            | extend MethodName = tostring(Properties['errormethodname'])
            | extend ErrorMessage = tostring(Properties['errormessage'])
            | extend Exceptionstack = tostring(Properties['exceptionstack'])
            | extend errcode1 = extract('^.*"errorCode":"(.*?)"', 1, Exceptionstack)
            | extend errcode2 = extract("^(.*?)\\n", 1, Exceptionstack)
            | extend errcode3 = extract("(.*)", 1, Exceptionstack)
            | extend errorcode = coalesce(errcode1, errcode2, errcode3, Fault)
            | summarize count() by errorcode
            | order by count_ desc
            | take 50
            """;
        var result = queryKusto(query);
        return result;
    }

    private static string queryKusto(string query)
    {
        // This method is not used in the current implementation, but can be used for custom queries
        var kcsb = new KustoConnectionStringBuilder("https://DDTelvscode.kusto.windows.net")
            .WithAadUserPromptAuthentication();
        using (var KustoClient = KustoClientFactory.CreateCslQueryProvider(kcsb))
        {
            var databaseName = "VSCodeExt";
            using var reader = KustoClient.ExecuteQuery(databaseName, query, null);
            var results = new List<string>();
            while (reader.Read())
            {
                // Process each row of the result
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                results.Add(JsonSerializer.Serialize(row));
            }
            return JsonSerializer.Serialize(results);
        }
    }

    public static void LogIntoKusto()
    {
        // cluster('https://DDTelvscode.kusto.windows.net').database('VSCodeExt').RawEventsVSCodeExt 
        var kcsb = new KustoConnectionStringBuilder("https://DDTelvscode.kusto.windows.net")
            .WithAadUserPromptAuthentication();
        using (var KustoClient = KustoClientFactory.CreateCslQueryProvider(kcsb))
        {
            var databaseName = "VSCodeExt";
            var query = """
                cluster('https://DDTelvscode.kusto.windows.net').database('VSCodeExt').RawEventsVSCodeExt 
                | where ExtensionName == 'fabric.vscode-fabric' or ExtensionName == 'fabric.vscode-fabric-functions'
                | summarize count() by EventName
                | order by count_ desc
                """;
            using var reader = KustoClient.ExecuteQuery(databaseName, query, null);
            while (reader.Read())
            {
                // Process each row of the result
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Console.Error.Write($"{reader.GetName(i)}: {reader.GetValue(i)} ");
                }
                Console.Error.WriteLine();
            }
        }
        // Use queryProvider to execute queries as needed
    }

}