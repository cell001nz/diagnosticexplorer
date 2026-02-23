﻿using DiagnosticExplorer.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiagnosticExplorer.DataAccess.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Username)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasIndex(a => a.Username)
            .IsUnique();

        builder.Property(a => a.Email)
            .HasMaxLength(256);

        builder.Property(a => a.IsProfileComplete)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.CreatedAt)
            .IsRequired();
    }
}

