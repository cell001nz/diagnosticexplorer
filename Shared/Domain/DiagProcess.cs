using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiagnosticExplorer.Domain;

namespace DiagnosticExplorer.Api.Domain;

public class DiagProcess
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public string? InstanceId { get; set; }
    public string Name { get; set; } = "";
    public string? UserName { get; set; } = "";
    public string? MachineName { get; set; } = "";

    public DateTime LastOnline { get; set; }
    public bool IsOnline { get; set; }
    public bool IsSending { get; set; }
    
    public DateTime LastReceived { get; set; }
}