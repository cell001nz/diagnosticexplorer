using DiagnosticExplorer.Api.Domain;

namespace DiagnosticExplorer.IO;

public interface IAccountIO
{

    Task<Account?> GetAccount(string id);
    Task<Account> SaveAccount(Account account);
}