# Copilot Instructions for MCPSample

## Architecture Overview

This is a **Model Context Protocol (MCP) server** built in C# that provides telemetry analysis tools for VS Code extensions, specifically Fabric extensions. The server exposes tools that can be called by MCP clients (like GitHub Copilot) to query Kusto databases and perform text analysis.

### Key Components

- **MCP Server**: Uses `ModelContextProtocol.Server` package with stdio transport
- **Tool Methods**: Static methods in `EchoTool` class decorated with `[McpServerTool]` attributes
- **Kusto Integration**: Queries VS Code extension telemetry via Azure Data Explorer
- **Dictionary Services**: Word analysis using `DictionaryLib_Calvin_Hsia` package

## Critical Patterns

### MCP Tool Definition Pattern
```csharp
[McpServerTool(ReadOnly = true), Description("Tool description")]
public static string MethodName(parameters...)
{
    // Use Console.Error.WriteLine for debugging - Console.WriteLine interferes with MCP protocol
    Console.Error.WriteLine($"Debug info");
    return JsonSerializer.Serialize(result);
}
```

### Kusto Query Construction
- Use raw string literals (`"""`) for multi-line KQL queries
- DateTime formatting: `{dateTime:yyyy-MM-ddTHH:mm:ssZ}`
- Dynamic arrays: `dynamic(["value"])` or `dynamic(null)` for optional filters
- Always serialize results as JSON arrays of JSON objects

### Error Handling for MCP
- **Never use `Console.WriteLine`** - it breaks MCP stdio communication
- Use `Console.Error.WriteLine` for debugging only
- Return serialized JSON strings, not exceptions
- Handle null/empty parameters gracefully with default values

## Development Workflow

### Build & Run
```bash
dotnet build                    # Build project
dotnet run --project MCPSample  # Run MCP server directly
```

### MCP Client Testing
The server is configured in `.vscode/mcp.json` and can be tested through VS Code's MCP integration. The server ID is `my-mcp-server-38b22a2a`.

### Adding New Tools
1. Add static method to `EchoTool` class
2. Decorate with `[McpServerTool]` and `Description` attributes
3. Use `ReadOnly = true` for query operations
4. Return JSON-serialized strings
5. Test via MCP client before committing

## Kusto Integration Specifics

### Connection Pattern
```csharp
var kcsb = new KustoConnectionStringBuilder("https://DDTelvscode.kusto.windows.net")
    .WithAadUserPromptAuthentication();
```

### Query Result Processing
- Read row-by-row with `reader.Read()`
- Build `Dictionary<string, object>` per row
- Serialize individual rows, then serialize the array
- Handle null values from Kusto gracefully

### Telemetry Query Patterns
- Filter by `ExtensionName` for specific extensions
- Use `ServerTimestamp between` for time ranges
- Extract error details from `Properties` JSON fields
- Use `summarize count() by` for aggregation

## Project Dependencies

- **Core**: .NET 9.0, Microsoft.Extensions.Hosting
- **MCP**: ModelContextProtocol package (preview)
- **Data**: Microsoft.Azure.Kusto.* packages for Azure Data Explorer
- **Utilities**: DictionaryLib_Calvin_Hsia for word analysis
- **JSON**: System.Text.Json for serialization

## Configuration Files

- `.vscode/mcp.json`: MCP server registration for VS Code
- `.vscode/tasks.json`: Build task configuration
- `MCPSample.csproj`: Package references and target framework

## Testing Approach

Test MCP tools through VS Code's chat interface or other MCP clients. The server provides both utility functions (word analysis) and telemetry queries (Fabric extension analytics).
