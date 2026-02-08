using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiagnosticExplorer.Util;

public static class TaskExtensions
{

    public static async Task<bool> Catch(this Task task, Action<Exception> handler)
    {
        try
        {
            await task;
            return true;
        }
        catch (Exception ex)
        {
            handler(ex);
            return false;
        }
    }
}