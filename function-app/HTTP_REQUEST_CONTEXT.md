# HttpRequest Context Access in Azure Functions Isolated Worker

## Problem
Azure Functions Isolated Worker process does **NOT** support `IHttpContextAccessor` because the function runs out-of-process and doesn't have direct access to the ASP.NET Core HttpContext.

## Solution
This solution uses a custom middleware and `AsyncLocal<T>` storage to capture and store the `HttpRequest` for the duration of each request, allowing access throughout the request pipeline without explicitly passing it.

## How It Works

### 1. HttpRequestContextHolder (Util/HttpRequestContextHolder.cs)
A static class that uses `AsyncLocal<HttpRequest>` to store the current request in a thread-safe manner that flows with async/await.

```csharp
public class HttpRequestContextHolder
{
    private static readonly AsyncLocal<HttpRequest?> _currentRequest = new();

    public static HttpRequest? Current
    {
        get => _currentRequest.Value;
        set => _currentRequest.Value = value;
    }
}
```

### 2. HttpRequestContextMiddleware (Middleware/HttpRequestContextMiddleware.cs)
Middleware that captures the HttpRequest at the start of each function execution and stores it in the AsyncLocal holder.

### 3. ApiBase.GetCurrentAccount()
A protected method in ApiBase that lazily loads the current account without requiring HttpRequest to be passed:

```csharp
protected async Task<Account> GetCurrentAccount()
{
    if (_currentAccount != null)
        return _currentAccount;
        
    var httpRequest = HttpRequestContextHolder.Current;
    if (httpRequest == null)
        throw new InvalidOperationException("HttpRequest is not available in the current context");
        
    _currentAccount = await GetLoggedInAccount(httpRequest);
    return _currentAccount;
}
```

## Usage

### Before (passing HttpRequest everywhere):
```csharp
[Function("GetProcesses")]
public async Task<IActionResult> GetProcesses(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Sites/{siteId}/Processes")] HttpRequest req, 
    int siteId)
{
    var account = await GetLoggedInAccount(req);  // Must pass req
    await VerifySiteAccess(account, siteId);
    // ... rest of code
}
```

### After (using GetCurrentAccount):
```csharp
[Function("GetProcesses")]
public async Task<IActionResult> GetProcesses(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Sites/{siteId}/Processes")] HttpRequest req, 
    int siteId)
{
    var account = await GetCurrentAccount();  // No need to pass req!
    await VerifySiteAccess(account, siteId);
    // ... rest of code
}
```

### Even Better - Use in Helper Methods:
```csharp
private async Task<bool> ValidateUserPermissions(int siteId)
{
    var account = await GetCurrentAccount();  // Works anywhere in the call chain!
    return account.HasAccessToSite(siteId);
}
```

## Benefits
1. ✅ No need to pass HttpRequest through method chains
2. ✅ Cleaner method signatures
3. ✅ Easy to get current user anywhere in the request pipeline
4. ✅ Thread-safe with async/await
5. ✅ Automatically cleaned up after each request

## Limitations
1. ⚠️ Only works for HTTP-triggered functions (not Timer, Queue, etc.)
2. ⚠️ HttpRequestContextHolder.Current will be null outside HTTP request context
3. ⚠️ Requires the middleware to be registered in Program.cs

## Registration
The middleware is registered in Program.cs:
```csharp
builder.ConfigureFunctionsWebApplication(workerApplication =>
{
    workerApplication.UseMiddleware<HttpRequestContextMiddleware>();
});
```

