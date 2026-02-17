using DiagnosticExplorer.DataAccess.Entities;
using DiagnosticExplorer.Domain;
using System.Linq.Expressions;

namespace DiagnosticExplorer.DataExtensions;

public static class WebSessionEntityUtil
{
    public static Expression<Func<WebSessionEntity, WebClient>> Projection =>
        entity => new WebClient
        {
            Id = entity.Id,
            AccountId = entity.AccountId,
            Subscriptions = entity.Subscriptions.Select(s => new WebProcSub
            {
                ProcessId = s.ProcessId,
                WebConnectionId = "",
                Date = s.RenewedAt
            }).ToArray()
        };
    
    private static readonly Func<WebSessionEntity, WebClient> CompiledProjection = Projection.Compile();
    
    extension(WebSessionEntity entity)
    {
        public WebClient ToDto()
        {
            return CompiledProjection(entity);
        }
    }
}

