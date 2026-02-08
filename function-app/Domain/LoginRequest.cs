using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticExplorer.Domain;

public class LoginRequest
{
    public string ClientId { get; set; }
    public string Secret { get; set; }
}