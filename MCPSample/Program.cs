using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

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
    
    [McpServerTool, Description("Gets a random english word")]
    public static string RandomWord()
    {
        var dict = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
        var randWord = dict.RandomWord();
        Console.Error.WriteLine($"From MyMCPServer Random word: {randWord}");
        // Don't use Console.WriteLine in MCP servers - it interferes with protocol communication
        // Use Console.Error.WriteLine for debugging instead, or remove entirely
        return randWord;
    }
}