﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;
using log4net.Util;

namespace DiagnosticExplorer.Log4Net;

[DiagnosticClass(AttributedPropertiesOnly = true, DeclaringTypeOnly = false)]
public class AsyncForwardingAppender : ForwardingAppender, IDisposable
{
    private FixFlags fixFlags = FixFlags.Partial;
    private AsyncProcessor _processor;

    public override void ActivateOptions()
    {
        base.ActivateOptions();

        InitializeAppenders();
        _processor = new AsyncProcessor(Overflow, MaxQueueSize, PerformAppend);
        _processor.Fix = Fix;
        _processor.Start();
    }

    public override void AddAppender(IAppender newAppender)
    {
        base.AddAppender(newAppender);
        SetAppenderFixFlags(newAppender);
    }

    [Property]
    public int MaxQueueSize { get; set; } = 1000;

    [Property]
    public BufferOverflowMode Overflow { get; set; } = BufferOverflowMode.Block;

    public FixFlags Fix { get; set; } = FixFlags.Partial;

    [Property]
    public int? CurrentQueueSize
    {
        get { return _processor?.QueueSize; }
    }


    private void InitializeAppenders()
    {
        foreach (var appender in Appenders)
        {
            SetAppenderFixFlags(appender);
        }
    }


    private void SetAppenderFixFlags(IAppender appender)
    {
        var bufferingAppender = appender as BufferingAppenderSkeleton;
        if (bufferingAppender != null)
        {
            bufferingAppender.Fix = Fix;
        }
    }


    protected override void Append(LoggingEvent loggingEvent)
    {
        EventsIn.Register(1);
        _processor.Append(loggingEvent);
    }

    protected override void Append(LoggingEvent[] loggingEvents)
    {
        EventsIn.Register(loggingEvents.Length);
        _processor.Append(loggingEvents);
    }

	
    protected override void OnClose()
    {
        _processor.Close();
        base.OnClose();
    }

    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _processor?.Dispose();
                _processor = null;
            }
            _disposed = true;
        }
    }


    // Use C# destructor syntax for finalization code.
    ~AsyncForwardingAppender()
    {
        // Simply call Dispose(false).
        Dispose(false);
    }
}