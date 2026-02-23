﻿using DiagnosticExplorer.DataAccess.Entities;
using DiagnosticExplorer.Domain;
using System.Linq.Expressions;

namespace DiagnosticExplorer.DataExtensions;

public static class AccountEntityUtil
{
    public static Expression<Func<AccountEntity, Account>> Projection =>
        entity => new Account
        {
            Id = entity.Id,
            Name = entity.Name,
            Email = entity.Email,
            IsProfileComplete = entity.IsProfileComplete
        };
    
    private static readonly Func<AccountEntity, Account> CompiledProjection = Projection.Compile();
    
    extension(AccountEntity entity)
    {
        public Account ToDto()
        {
            return CompiledProjection(entity);
        }
    }
}

