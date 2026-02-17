using DiagnosticExplorer.DataAccess.Entities;
using DiagnosticExplorer.Domain;
using System.Linq.Expressions;

namespace DiagnosticExplorer.DataExtensions;

public static class SiteEntityUtil
{
    public static Expression<Func<SiteEntity, Site>> Projection =>
        entity => new Site
        {
            Id = entity.Id,
            Name = entity.Name,
            Code = entity.Code,
            Secrets = entity.Secrets.Select(s => new Secret
            {
                Id = s.Id,
                Name = s.Name,
                Value = s.Value,
                Hash = s.Hash
            }).ToList()
        };
    
    private static readonly Func<SiteEntity, Site> CompiledProjection = Projection.Compile();

    public static Site ToDto(this SiteEntity entity)
    {
        return CompiledProjection(entity);
    }
}

