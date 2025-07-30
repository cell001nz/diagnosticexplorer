using System.Diagnostics;
using DiagnosticExplorer.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace DiagnosticExplorer.IO;

public interface IProcessIO
{
    Task<DiagProcess?> GetProcessForConnectionId(string connectionId);
    Task SetProcessSending(string processId, bool isSending);
    Task<DiagProcess[]> GetProcessesForSite(string siteId);
    Task SetOnline(string processId, string processSiteId);
    Task SetOffline(string processId, string siteId);
    Task<DiagProcess[]> GetCandidateProcesses(string siteId, string processName, string machineName, string userName);
    Task<DiagProcess?> GetProcess(string processId, string siteId);
    Task<DiagProcess> SaveProcess(DiagProcess process);
}