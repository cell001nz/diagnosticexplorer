#region Copyright

// Diagnostic Explorer, a .Net diagnostic toolset
// Copyright (C) 2010 Cameron Elliot
// 
// This file is part of Diagnostic Explorer.
// 
// Diagnostic Explorer is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Diagnostic Explorer is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with Diagnostic Explorer.  If not, see <http://www.gnu.org/licenses/>.
// 
// http://diagexplorer.sourceforge.net/

#endregion

using System;
using log4net.Appender;
using log4net.Core;

namespace DiagnosticExplorer.Log4Net;

public class DiagnosticAppender : AppenderSkeleton
{
    private EventSink _sink;
    private const int MaxMsgLength = 150;
    private const string appenderKey = "DiagnosticAppenderHandled";

    public DiagnosticAppender()
    {
        SinkName = "System";
        SinkCategory = "System";
        ExcludeAlreadyLogged = true;
    }

    public bool ExcludeAlreadyLogged { get; set; }

    public string SinkName { get; set; }

    public string SinkCategory { get; set; }

    protected override void Append(LoggingEvent loggingEvent)
    {
        _sink ??= EventSinkRepo.Default.GetSink(SinkName, SinkCategory);

        if (ExcludeAlreadyLogged)
        {
            if (loggingEvent.Properties.Contains(appenderKey))
                return;

            loggingEvent.Properties[appenderKey] = true;
        }

        string detail = RenderLoggingEvent(loggingEvent);
        if (!ReferenceEquals(loggingEvent.MessageObject, loggingEvent.ExceptionObject))
            detail += Environment.NewLine + loggingEvent.ExceptionObject;

        string message = GetMessage(loggingEvent);

        _sink.LogEvent(GetLogLevel(loggingEvent.Level), message, detail);
    }

    private string GetMessage(LoggingEvent loggingEvent)
    {
        string message = loggingEvent.RenderedMessage;
        int index = message.IndexOf("\n");
        if (index != -1)
            message = message.Substring(0, index);
				
        if (message.Length > MaxMsgLength)
            message = message.Substring(0, MaxMsgLength) + "...";

        return message;
    }

    private LogLevel GetLogLevel(Level level)
    {
        if (level.Value <= Level.Verbose.Value)
            return LogLevel.Verbose;
        
        if (level.Value <= Level.Debug.Value)
            return LogLevel.Debug;
        
        if (level.Value <= Level.Info.Value)
            return LogLevel.Info;
        
        if (level.Value <= Level.Notice.Value)
            return LogLevel.Notice;
        
        if (level.Value <= Level.Warn.Value)
            return LogLevel.Warn;
        
        if (level.Value <= Level.Error.Value)
            return LogLevel.Error;
        
        if (level.Value <= Level.Critical.Value)
            return LogLevel.Critical;
        
            return LogLevel.Fatal;
}    
    
    private EventSeverity GetSeverity(Level level)
    {
        switch (level.Name.ToUpper())
        {
            case "ALL":
            case "TRACE":
            case "DEBUG":
            case "FINE":
            case "FINER":
            case "FINEST":
            case "INFO":
            case "VERBOSE":
            case "OFF":
                return EventSeverity.Low;

            case "WARN":
                return EventSeverity.Medium;

            case "CRITICAL":
            case "EMERGENCY":
            case "ERROR":
            case "FATAL":
            case "SEVERE":
                return EventSeverity.High;

            default:
                return EventSeverity.High;
        }
    }
}