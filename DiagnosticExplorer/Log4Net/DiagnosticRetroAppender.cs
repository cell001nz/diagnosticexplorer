using System;
using System.Diagnostics;
using System.Reflection;
using log4net.Appender;
using log4net.Core;

namespace DiagnosticExplorer.Log4Net;

public class DiagnosticRetroAppender : AppenderSkeleton
{
    private string _version;
    private string _process;

    private static Action<DiagnosticMsg> _loggingAction;

    public string Environment { get; set; }

    public static void SetLoggingAction(Action<DiagnosticMsg> action)
    {
        _loggingAction = action;
    }


    public DiagnosticRetroAppender()
    {
        _version = Assembly.GetEntryAssembly()?.GetName().Version.ToString();
        _process = Process.GetCurrentProcess().ProcessName;
    }

    public override void ActivateOptions()
    {
        base.ActivateOptions();

        DiagnosticManager.Register(this, Name, "Log4Net");
    }

    protected override void Append(LoggingEvent loggingEvent)
    {
        // Debug.WriteLine("DiagnosticRetroAppender.Append");
        // Debug.WriteLine("DiagnosticRetroAppender.PerformSend");
        DiagnosticMsg msg = new() {
            Level = loggingEvent.Level.Value,
            Date = DateTime.UtcNow,
            Machine = System.Environment.MachineName,
            User = System.Environment.UserName,
            Environment = Environment,
            Category = loggingEvent.LoggerName,
            Process = $"{_process} {_version}",
            Message = GetMessage(loggingEvent)
        };

        EventsIn.Register(1);
        _loggingAction?.Invoke(msg);
    }


    [RateProperty(ExposeRate = false, ExposeTotal = true)]
    public RateCounter EventsIn { get; set; } = new RateCounter(3);

    private string GetMessage(LoggingEvent loggingEvent)
    {
        string message = RenderLoggingEvent(loggingEvent);

        if (!ReferenceEquals(loggingEvent.MessageObject, loggingEvent.ExceptionObject))
            message += System.Environment.NewLine + loggingEvent.ExceptionObject;

        return message;
    }

    protected override void OnClose()
    {
        base.OnClose();
        DiagnosticManager.Unregister(this);
    }

}