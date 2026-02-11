using System.Diagnostics;
using DiagnosticExplorer.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace DiagnosticExplorer.IO;

public interface IProcessIO
{
    Task<DiagProcess?> GetProcessForConnectionId(string connectionId);
    Task SetProcessSending(string processId, string siteId, bool isSending);
    Task<DiagProcess[]> GetProcessesForSite(string siteId);
    Task SetLastReceived(string processId, string processSiteId, DateTime date);
    Task SetOnline(string processId, string siteId, DateTime date);
    Task SetOffline(string processId, string siteId);
    Task<DiagProcess[]> GetCandidateProcesses(string siteId, string processName, string machineName, string userName);
    Task<DiagProcess?> GetProcess(string processId, string siteId);
    Task<DiagProcess> SaveProcess(DiagProcess process);
    Task Delete(string processId, string siteId);
    Task SaveWebSub(WebProcSub sub);
    Task DeleteWebSub(WebProcSub sub);
    Task SetConnectionId(string processId, string siteId, string connectionId);
    Task<DiagProcess[]> GetStaleOnlineProcesses(DateTime cutoffTime);
}