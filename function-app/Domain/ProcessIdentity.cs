using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticExplorer.Domain;

public class ProcessIdentity
{
    public ProcessIdentity()
    {
    }

    public ProcessIdentity(string processId, string siteId)
    {
        ProcessId = processId;
        SiteId = siteId;
    }

    public string ProcessId { get; set; } = "";
    public string SiteId { get; set; } = "";
}