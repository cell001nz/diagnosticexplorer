﻿namespace DiagnosticExplorer.DataAccess.Entities;

public class AccountEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsProfileComplete { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SiteEntity> Sites { get; set; } = [];

    public ICollection<ProcessEntity> Processes { get; set; } = [];
    public ICollection<WebSessionEntity> Sessions { get; set; } = [];
}