# MCPSample

A **Model Context Protocol (MCP) server** built in C# that provides telemetry analysis tools for VS Code extensions, specifically Fabric extensions. This server exposes tools that can be called by MCP clients (like GitHub Copilot) to query Kusto databases and perform text analysis.

## Features

### 🔍 Telemetry Analysis Tools
- **Fabric Extension Telemetry**: Query VS Code extension usage data from Azure Data Explorer
- **Error Analysis**: Retrieve and analyze common Fabric extension errors
- **Custom Kusto Queries**: Execute arbitrary KQL queries against telemetry databases

### 📝 Text Analysis Tools
- **Random Word Generator**: Get random English words from dictionary
- **Sub-word Analysis**: Find all possible words that can be made from letters of a given word

## Architecture

### Key Components
- **MCP Server**: Uses `ModelContextProtocol.Server` package with stdio transport
- **Tool Methods**: Static methods in `MyMCPSampleClass` class decorated with `[McpServerTool]` attributes
- **Kusto Integration**: Queries VS Code extension telemetry via Azure Data Explorer
- **Dictionary Services**: Word analysis using `DictionaryLib_Calvin_Hsia` package

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Access to Azure Data Explorer (Kusto) cluster: `https://DDTelvscode.kusto.windows.net`

### Building and Running

```bash
# Build the projectZ
dotnet build

# Run the MCP server
dotnet run --project MCPSample
```

### VS Code Integration

The server is configured for use with VS Code through `.vscode/mcp.json`:

```json
{
    "servers": {
        "my-mcp-server-38b22a2a": {
            "type": "stdio",
            "command": "dotnet",
            "args": ["run", "--project", "C:\\Repos\\MCPSample\\MCPSample\\MCPSample.csproj"]
        }
    }
}
```

## Available Tools

### 🎲 `RandomWord()`
Returns a random English word from the dictionary.

### 🔤 `GetSubWords(string word)`
Returns all valid English words that can be formed using the letters from the input word.

### 📊 `LogIntoKustoAndGetTelemetry(DateTime startDate, DateTime endDate, string? coreVersion, string? udfVersion)`
Queries Fabric extension telemetry for event names and counts within a date range, optionally filtered by version.

### ❌ `GetFabricCommonErrors()`
Retrieves the most common error types from Fabric extensions.

### 🔍 `QueryKusto(string query)`
Executes a custom KQL query against the telemetry database.

## Example Usage

Once connected through an MCP client:

```
Sample Copilot Prompts:

Give a random word
Random word > 7 letters
find all anagrams of discounter
What are the subwords of “someword”
Is “openers” a subword of Personalize?

what were most fabric events for core version 0.22.3
what were most events in first week of june?
what are come common fabric errors?

For Kusto-MCP
what data is available from kusto
How many different people use fabric extension in June
What is the publish success rate?
Show me the query



```

## Development

### Adding New Tools
1. Add a static method to the `MyMCPSampleClass` class
2. Decorate with `[McpServerTool]` and `Description` attributes
3. Use `ReadOnly = true` for query operations
4. Return JSON-serialized strings
5. Use `Console.Error.WriteLine()` for debugging (never `Console.WriteLine()`)

### Error Handling
- **Never use `Console.WriteLine()`** - it breaks MCP stdio communication
- Use `Console.Error.WriteLine()` for debugging only
- Return serialized JSON strings, not exceptions
- Handle null/empty parameters gracefully

## Dependencies

- **Core**: .NET 9.0, Microsoft.Extensions.Hosting
- **MCP**: ModelContextProtocol package (preview)
- **Data**: Microsoft.Azure.Kusto.* packages for Azure Data Explorer
- **Utilities**: DictionaryLib_Calvin_Hsia for word analysis
- **JSON**: System.Text.Json for serialization

## License

This project is for internal telemetry analysis and development purposes.
