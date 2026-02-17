using DiagnosticExplorer.DataAccess.Entities;
using DiagnosticExplorer.Domain;
using System.Linq.Expressions;

namespace DiagnosticExplorer.DataExtensions;

public static class WebSubcriptionEntityUtil
{
    public static Expression<Func<WebSubcriptionEntity, WebProcSub>> Projection =>
        entity => new WebProcSub
        {
            ProcessId = entity.ProcessId,
            WebConnectionId = "",
            Date = entity.RenewedAt
        };
    
    private static readonly Func<WebSubcriptionEntity, WebProcSub> CompiledProjection = Projection.Compile();

    public static WebProcSub ToDto(this WebSubcriptionEntity entity)
    {
        return CompiledProjection(entity);
    }
}

