using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticExplorer.Domain;

internal static class Messages
{
    
    public static WebMessageNames Web { get; } = new();
    public static ProcessClientNames Process { get; } = new();


    internal class ProcessClientNames
    {
        public string StartSending => nameof(StartSending);
        public string StopSending => nameof(StopSending);
        public string SetRenewTime => nameof(SetRenewTime);
    }

    internal class WebMessageNames
    {
        public string ReceiveProcess => nameof(ReceiveProcess);
        public string ReceiveDiagnostics => nameof(ReceiveDiagnostics);
        public string ClearEventStream => nameof(ClearEventStream);
        public string StreamEvents => nameof(StreamEvents);
    }
    
}