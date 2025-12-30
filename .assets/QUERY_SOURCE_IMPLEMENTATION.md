# Query Source Implementation Summary

## Overview

This implementation adds support for capturing source code location information for database queries in Sentry's .NET SDK. This enables the "Connecting Queries with Code" feature in Sentry's Queries module, showing developers exactly which line of code triggered a slow database query.

## Implementation Details

### 1. Configuration Options (SentryOptions.cs)

Two new public properties were added:

- **`EnableDbQuerySource`** (bool, default: true)
  - Enables/disables query source capture
  - When enabled, the SDK captures source file, line number, function name, and namespace for database queries
  
- **`DbQuerySourceThresholdMs`** (int, default: 100ms)
  - Minimum query duration before source location is captured
  - Helps minimize performance overhead by only capturing slow queries
  - Set to 0 to capture all queries

### 2. Core Logic (QuerySourceHelper.cs)

New internal static helper class that implements stack walking logic:

**Key Features:**
- Captures current stack trace with file information (requires PDB files)
- Filters frames to find first "in-app" frame by:
  - Skipping Sentry SDK frames (`Sentry.*`)
  - Skipping EF Core frames (`Microsoft.EntityFrameworkCore.*`)
  - Skipping System.Data frames (`System.Data.*`)
  - Using existing `InAppInclude`/`InAppExclude` logic via `SentryStackFrame.ConfigureAppFrame()`
  
- Sets span extra data with OpenTelemetry semantic conventions:
  - `code.filepath` - Source file path (made relative when possible)
  - `code.lineno` - Line number
  - `code.function` - Function/method name
  - `code.namespace` - Namespace

**Performance Considerations:**
- Only runs when feature is enabled
- Only runs when query duration exceeds threshold
- Gracefully handles missing PDB files
- All exceptions are caught and logged

### 3. Integration Points

**EF Core Integration** (`EFDiagnosticSourceHelper.cs`):
- Modified `FinishSpan()` to call `QuerySourceHelper.TryAddQuerySource()` before finishing span
- Works with EF Core diagnostic events

**SqlClient Integration** (`SentrySqlListener.cs`):
- Modified `FinishCommandSpan()` to call `QuerySourceHelper.TryAddQuerySource()` before finishing span
- Works with System.Data.SqlClient and Microsoft.Data.SqlClient

### 4. Testing

**Unit Tests** (`QuerySourceHelperTests.cs`):
- Tests feature enable/disable
- Tests duration threshold filtering
- Tests in-app frame filtering logic
- Tests exception handling
- Tests InAppInclude/InAppExclude respect

**Integration Tests** (`QuerySourceTests.cs`):
- Tests EF Core query source capture
- Tests SqlClient query source capture
- Tests threshold behavior
- Tests feature disable behavior
- Verifies actual database queries capture correct source information

## Usage

### Basic Usage

Query source capture is **enabled by default**. No code changes required:

```csharp
var options = new SentryOptions
{
    Dsn = "your-dsn",
    TracesSampleRate = 1.0,
    // Query source is enabled by default
};
```

### Customization

```csharp
var options = new SentryOptions
{
    Dsn = "your-dsn",
    TracesSampleRate = 1.0,
    
    // Disable query source capture
    EnableDbQuerySource = false,
    
    // OR adjust threshold (only capture queries > 500ms)
    DbQuerySourceThresholdMs = 500
};
```

## Requirements

- **PDB Files**: Debug symbols (PDB files) must be deployed with the application
  - This is the default behavior for .NET publish (PDBs are included)
  - Works in both Debug and Release builds as long as PDBs are present
  
- **Sentry Backend**: Backend must support `code.*` span attributes (already supported)

## Graceful Degradation

If PDB files are not available:
- Stack frames will not have file information
- Query source data will not be captured
- No errors or exceptions thrown
- Queries still tracked normally without source location

## Performance Impact

- **Negligible when below threshold**: Just a timestamp comparison
- **Minimal when above threshold**: Stack walking is fast (~microseconds)
- **Recommended threshold**: 100ms (default) balances usefulness with overhead
- **For very high-traffic apps**: Consider raising threshold to 500ms or 1000ms

## Example Output

When a slow query is detected, the span will include:

```json
{
  "op": "db.query",
  "description": "SELECT * FROM Users WHERE Id = @p0",
  "data": {
    "db.system": "sqlserver",
    "db.name": "MyDatabase",
    "code.filepath": "src/MyApp/Services/UserService.cs",
    "code.function": "GetUserAsync",
    "code.lineno": 42,
    "code.namespace": "MyApp.Services.UserService"
  }
}
```

This information appears in Sentry's Queries module, allowing developers to click through to the exact line of code.

## Architecture Decisions

### Why Stack Walking Instead of Source Generators?

1. **Simplicity**: Stack walking is straightforward and leverages existing .NET runtime capabilities
2. **No Build-Time Complexity**: No need for Roslyn analyzers or source generators
3. **Works Today**: PDB files are commonly deployed in .NET applications
4. **Minimal Changes**: Small, focused implementation in existing integration packages

### Why Skip Frames?

The `skipFrames` parameter (default 2) skips:
1. The `TryAddQuerySource` method itself
2. The `FinishSpan` method that calls it

This ensures we capture the actual application code that triggered the query, not internal SDK frames.

### Why Use Existing InApp Logic?

Reusing `SentryStackFrame.ConfigureAppFrame()` ensures:
- Consistent behavior with other Sentry features
- Respect for user-configured `InAppInclude`/`InAppExclude`
- No duplication of complex frame filtering logic

## Future Enhancements

1. **Caching**: Cache stack walk results per call site for better performance
2. **Source Generators**: Add compile-time source location capture for zero runtime overhead
3. **Extended Support**: Extend to HTTP client, Redis, and other operations
4. **Server-Side Resolution**: Send frame tokens to Sentry for server-side PDB lookup

## Related Links

- [GitHub Issue #3227](https://github.com/getsentry/sentry-dotnet/issues/3227)
- [Sentry Docs: Query Sources](https://docs.sentry.io/product/insights/backend/queries/#query-sources)
- [Python SDK Implementation](https://github.com/getsentry/sentry-python/blob/master/sentry_sdk/tracing_utils.py#L186)
- [OpenTelemetry Code Attributes](https://github.com/open-telemetry/semantic-conventions/blob/main/docs/general/attributes.md#source-code-attributes)
