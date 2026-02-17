using DiagnosticExplorer.DataAccess.Entities;
using DiagnosticExplorer.Api.Domain;
using System.Linq.Expressions;

namespace DiagnosticExplorer.DataExtensions;

public static class ProcessEntityUtil
{
    public static Expression<Func<ProcessEntity, DiagProcess>> Projection =>
        entity => new DiagProcess
        {
            Id = entity.Id,
            SiteId = entity.SiteId,
            InstanceId = entity.InstanceId,
            Name = entity.Name,
            UserName = entity.UserName,
            MachineName = entity.MachineName,
            IsOnline = entity.IsOnline,
            IsSending = entity.IsSending,
            LastOnline = entity.LastOnline,
            LastReceived = entity.LastReceived
        };
    
    private static readonly Func<ProcessEntity, DiagProcess> CompiledProjection = Projection.Compile();
    
    extension(ProcessEntity entity)
    {
        public DiagProcess ToDto()
        {
            return CompiledProjection(entity);
        }
    }

}