using DiagnosticExplorer.DataAccess.Entities;
using DiagnosticExplorer.Domain;
using System.Linq.Expressions;

namespace DiagnosticExplorer.DataExtensions;

public static class SecretEntityUtil
{
    public static Expression<Func<SecretEntity, Secret>> Projection =>
        entity => new Secret
        {
            Id = entity.Id,
            Name = entity.Name,
            Value = entity.Value,
            Hash = entity.Hash
        };
    
    private static readonly Func<SecretEntity, Secret> CompiledProjection = Projection.Compile();

    public static Secret ToDto(this SecretEntity entity)
    {
        return CompiledProjection(entity);
    }
}

