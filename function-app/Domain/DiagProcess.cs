using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticExplorer.Api.Domain;

public class DiagProcess
{
    public string Id { get; set; }
    
    public string SiteId { get; set; }

    public string InstanceId { get; set; }

    public string ProcessName { get; set; }
    public string ConnectionId { get; set; }

    public string UserName { get; set; }
    public string MachineName { get; set; }

    public DateTime LastOnline { get; set; }

    public bool IsOnline { get; set; }
    public bool IsSending { get; set; }

    public SinkMarker SinkMarker { get; set; } = new();

    public DateTime LastReceived { get; set; }
    public DateTime LastRequested { get; set; }

}

public class SinkMarker
{
    public string InstanceId { get; set; }
    public long EventId { get; set; }
}
