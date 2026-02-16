﻿using DiagnosticExplorer.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiagnosticExplorer.DataAccess.Configurations;

public class WebSessionConfiguration : IEntityTypeConfiguration<WebSessionEntity>
{
    public void Configure(EntityTypeBuilder<WebSessionEntity> builder)
    {
        builder.ToTable("web_sessions");

        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.ConnectionId).IsUnique();
        builder.HasIndex(s => s.AccountId);
        
        builder.HasOne(x => x.Account)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.AccountId)
            .HasConstraintName("FK_WebSessions_Account")
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}