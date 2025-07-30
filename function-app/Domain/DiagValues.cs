using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticExplorer.Domain;

public class DiagValues
{
    public string Id { get; set; }
    public string SiteId { get; set; }
    public DateTime Date { get; set; }
    public string Response { get; set; }
    
}