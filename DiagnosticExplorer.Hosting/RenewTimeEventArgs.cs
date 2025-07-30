using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticExplorer.Hosting;

public class RenewTimeEventArgs(TimeSpan time) : EventArgs
{
    public TimeSpan Time { get; } = time;
}