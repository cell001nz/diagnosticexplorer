﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;
using log4net.Util;

namespace DiagnosticExplorer.Log4Net;

/// <summary>
/// This appender takes care of falling back to another appender if appending causes
/// an error
/// </summary>
[DiagnosticClass(AttributedPropertiesOnly = true, DeclaringTypeOnly = false)]
public class FallbackAppender : ForwardingAppenderBase
{

    protected override void Append(LoggingEvent loggingEvent)
    {
        if (loggingEvent == null)
            throw new ArgumentNullException(nameof(loggingEvent));

        EventsIn.Register(1);

        PerformAppend(loggingEvent);
    }

    protected override void Append(LoggingEvent[] loggingEvents)
    {
        if (loggingEvents == null)
            throw new ArgumentNullException(nameof(loggingEvents));

        if (loggingEvents.Length == 0)
            throw new ArgumentException("loggingEvents array must not be empty", nameof(loggingEvents));

        if (loggingEvents.Length == 1)
        {
            PerformAppend(loggingEvents[0]);
            return;
        }

        PerformAppend(loggingEvents);
    }

    protected void PerformAppend(LoggingEvent loggingEvent)
    {
        Queue<AppenderProxy> proxyQueue = new Queue<AppenderProxy>(Proxies);

        while (proxyQueue.Count > 0)
        {
            AppenderProxy proxy = proxyQueue.Dequeue();

            if (proxy.TryAppend(loggingEvent))
            {
                EventsOut.Register(1);
                break;
            }
            EventsErrored.Register(1);
            RecordAppenderError(proxyQueue, proxy);
        }
    }


    protected void PerformAppend(LoggingEvent[] loggingEvents)
    {
        EventsIn.Register(loggingEvents.Length);

        var appenderQueue = new Queue<AppenderProxy>(Proxies);
        while (appenderQueue.Count > 0)
        {
            AppenderProxy appender = appenderQueue.Dequeue();

            if (appender.TryAppend(loggingEvents))
            {
                EventsOut.Register(loggingEvents.Length);
                break;
            }

            EventsErrored.Register(loggingEvents.Length);
            RecordAppenderError(appenderQueue, appender);
        }
    }

    private void RecordAppenderError(Queue<AppenderProxy> appenderQueue, AppenderProxy appender)
    {
        ForwardingAppenderBase.LogLogError(GetType(), $"appender [{appender.Appender.Name}] has an error.");
        if (appenderQueue.Count > 0)
        {
            var nextAppender = appenderQueue.Peek();
            LogLog.Debug(GetType(), $"Chaining through to appender [{nextAppender.Appender.Name}]");
        }
        else
            ForwardingAppenderBase.LogLogError(GetType(), "No more appenders exist to chain through to");
    }
}