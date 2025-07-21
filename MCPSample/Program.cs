using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using Kusto.Data;
using Kusto.Data.Net.Client;

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
public static class MyMCPSampleClass
{

    [McpServerTool(ReadOnly = true), Description("Gets a random English word")]
    public static string RandomWord()
    {
        var dict = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
        var randWord = dict.RandomWord();
        Console.Error.WriteLine($"From MyMCPServer Random word: {randWord}");
        // Don't use Console.WriteLine in MCP servers - it interferes with protocol communication
        // Use Console.Error.WriteLine for debugging instead, or remove entirely
        return randWord;
    }

    [McpServerTool(ReadOnly = true), Description("Gets the words made from the letters of a word")]
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

    [McpServerTool(ReadOnly = true), Description("Log into Kusto and query Fabric Telemetry for event names and counts")]
    public static string LogIntoKustoAndGetTelemetry(DateTime startDate, DateTime endDate, string? coreVersion = null, string? udfVersion = null)
    {
        var coreVersionFilter = string.IsNullOrEmpty(coreVersion) ? "dynamic(null)" : $"dynamic([\"{coreVersion}\"])";
        var udfVersionFilter = string.IsNullOrEmpty(udfVersion) ? "dynamic(null)" : $"dynamic([\"{udfVersion}\"])";
        
        var query = $"""
            let _endTime = datetime({endDate:yyyy-MM-ddTHH:mm:ssZ});
            let _startTime = datetime({startDate:yyyy-MM-ddTHH:mm:ssZ});
            let _CoreVersion = {coreVersionFilter};
            let _UDFVersion = {udfVersionFilter};
            cluster('https://DDTelvscode.kusto.windows.net').database('VSCodeExt').RawEventsVSCodeExt
            | where ServerTimestamp between (['_startTime'] .. ['_endTime']) // Time range filtering
            | where ExtensionName == 'fabric.vscode-fabric' or  ExtensionName  == 'fabric.vscode-fabric-functions'
            | where iif(ExtensionName == 'fabric.vscode-fabric', (isnull(_CoreVersion) or ExtensionVersion in (_CoreVersion)), (isnull(_UDFVersion) or ExtensionVersion in (_UDFVersion)))
            | summarize count() by EventName
            | order  by count_ desc
        """;
        Console.Error.WriteLine($"From MyMCPServer LogIntoKustoAndGetTelemetry startDate: {startDate} endDate: {endDate}\n{query}");
//         var queryx = $@"cluster('https://DDTelvscode.kusto.windows.net').database('VSCodeExt').RawEventsVSCodeExt
//     | where ExtensionName == 'fabric.vscode-fabric' or  ExtensionName  == 'fabric.vscode-fabric-functions'
//     | summarize count() by EventName
//     | order  by count_ desc
// ";
        var result = queryKusto(query);
        return result;
    }

    [McpServerTool, Description("Gets common Fabric extension errors")]
    public static string GetFabricCommonErrors()
    {
        Console.Error.WriteLine("From MyMCPServer GetFabricCommonErrors");
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

    [McpServerTool(ReadOnly = true), Description("Query Kusto for VSCode extension telemetry")]
    public static string QueryKusto(string query)
    {
        Console.Error.WriteLine($"From MyMCPServer QueryKusto query: {query}");
        var result = queryKusto(query);
        return result;
    }

    private static string queryKusto(string query)
    {
        try
        {
            // query = """
            //     cluster('https://DDTelvscode.kusto.windows.net').database('VSCodeExt').RawEventsVSCodeExt
            //     | summarize Count()
            // """;
            // throw new NotImplementedException();
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
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error executing Kusto query: {ex.Message}");
            var errorResult = new { error = ex.Message, type = ex.GetType().Name };
            return JsonSerializer.Serialize(errorResult);
        }
    }
}