using System.Text;
using System.Text.Json;
using DiagnosticExplorer.DataAccess;
using DiagnosticExplorer.DataExtensions;
using DiagnosticExplorer.Domain;
using DiagnosticExplorer.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace DiagnosticExplorer.Endpoints;

public class ApiBase
{
    protected readonly ILogger _logger;
    protected const string PROCESS_HUB = "process";
    protected const string WEB_HUB = "web";
    protected const string MESSAGES = "messages";
    
    protected const int PROCESS_RENEW_TIME_MILLIS = 10_000;
    protected const int PROCESS_STALE_TIME_MILLIS = 20_000;
    protected const int DIAG_SEND_FREQ_MILLIS = 2_000;

    
    protected DiagDbContext _context { get; }

    private Account? _currentAccount;
    
    /// <summary>
    /// Gets the currently logged-in account. Lazily loads on first access.
    /// Uses HttpRequestContextHolder to access the current request without explicit passing.
    /// </summary>
    protected async Task<Account> GetCurrentAccount(HttpRequest request)
    {
        if (_currentAccount != null)
            return _currentAccount;
            
        _currentAccount = await GetLoggedInAccount(request);
        return _currentAccount;
    }

    public ApiBase(ILogger logger, DiagDbContext context)
    {
        _logger = logger;
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Extracts the authenticated username from the x-ms-client-principal header.
    /// </summary>
    protected ClientPrincipal GetClientPrincipal(SignalRInvocationContext invocationContext)
    {
        return GetClientPrincipal(invocationContext.UserId);
    }
    
    protected ClientPrincipal GetClientPrincipal(HttpRequest req)
    {
        if (!req.Headers.TryGetValue("x-ms-client-principal", out var header))
            throw new ApplicationException($"x-ms-client-principal header not found");

        return GetClientPrincipal(header.FirstOrDefault());
    }

    protected ClientPrincipal GetClientPrincipal(string? principalEncoded)
    {
        if (principalEncoded == null)
            throw new ApplicationException($"x-ms-client-principal header not found");

        byte[] decodedBytes = Convert.FromBase64String(principalEncoded);
        string json = Encoding.UTF8.GetString(decodedBytes);

        return JsonSerializer.Deserialize<ClientPrincipal>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

 

    protected async Task<Account> GetLoggedInAccount(HttpRequest request)
    {
        ClientPrincipal cp = GetClientPrincipal(request);

        return await GetLoggedInAccount(cp);
    }

       protected async Task VerifySiteAccess(Account account, int siteId)
      {
          bool found = await _context.Sites
              .AnyAsync(s => s.Id == siteId && s.AccountId == account.Id);
            
          if (!found)
              throw new ApplicationException($"Requested site either does not exist or you do not have access to it");
          
          // await DiagIO.Site.GetSiteForUser(siteId, cp.UserId);
      }

      protected async Task<Account> GetLoggedInAccount(ClientPrincipal cp)
      {
          _logger.LogWarning($"Getting account for user {cp.UserId}");

          var account = await _context.Accounts.Where(a => a.Username == cp.UserId)
                            .Select(AccountEntityUtil.Projection)
                            .FirstOrDefaultAsync()
                        ?? throw new ApplicationException("Current user not found");
          _logger.LogWarning($"Found account for user {cp.UserId}: {account.Id}");
          return account;
      }

      protected T DeserialiseBase64Protobuf<T>(string strData)
      {
          byte[] data = Convert.FromBase64String(strData);
          return ProtobufUtil.Decompress<T>(data);
      }

      public class DualHubOutput
    {
        [SignalROutput(HubName = WEB_HUB)]
        public List<object> WebClient { get; } = [];
        
        [SignalROutput(HubName = PROCESS_HUB)]
        public List<object> ProcessClient { get; } = [];
    }
    
}
