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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using DiagnosticExplorer.Util;
using log4net.Core;

namespace DiagnosticExplorer;

public class EventSink : IDisposable
{
    public const int MaxMessages = 1000;
    private const int MaxLength = 102400;
    private static TimeSpan messageLife = TimeSpan.FromMinutes(30);
    private static WeakReferenceHash<EventSink> sinks = new();
    private static Timer _timer;
    private EventSinkRepo _repo;

        
    private static void PurgeAllSinks(object sender)
    {
        try
        {
            foreach (EventSink sink in sinks.GetItems())
                sink.Purge();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    static EventSink()
    {
        _timer = new Timer(PurgeAllSinks, null, 0, 20000);
    }

    internal EventSink(EventSinkRepo repo, string name, string category)
    {
        _repo = repo;
        Name = name;
        Category = category;
    }

    ~EventSink()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            sinks.Remove(Name);
    }

    public string Name { get; }

    public string Category { get; }


    private long _sinkSeq = 0;
    private static long _procSeq = 0;

    public ConcurrentQueue<SystemEvent> Events { get; } = new();

    private void Purge()
    {
        try
        {
            while (Events.Count > MaxMessages)
                if (!Events.TryDequeue(out _))
                    break;

            while (Events.TryPeek(out var evt) && ShouldPurge(evt))
                if (!Events.TryDequeue(out _))
                    break;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private bool ShouldPurge(SystemEvent evt)
    {
        return DateTime.UtcNow - evt.Date > messageLife;
    }

	
    public void Info(string message, string detail = null)
    {
        LogEvent(Level.Info.Value, message, detail);
    }

    public void Notice(string message, string detail = null)
    {
        LogEvent(Level.Notice.Value, message, detail);
    }

    public void Warn(string message, string detail = null)
    {
        LogEvent(Level.Warn.Value, message, detail);
    }

    public void Error(string message, string detail = null)
    {
        LogEvent(Level.Error.Value, message, detail);
    }

    public void Fatal(string message, string detail = null)
    {
        LogEvent(Level.Fatal.Value, message, detail);
    }

    public void LogEvent(int level, string message, string detail)
    {
        try
        {
            CleanMessageAndDetail(ref message, ref detail);

            SystemEvent evt = new();
            evt.SinkSeq = Interlocked.Increment(ref _sinkSeq);
            evt.ProcSeq = Interlocked.Increment(ref _procSeq);
            evt.Date = DateTime.UtcNow;
            evt.Level = level;
            evt.Sink = Name;
            evt.Cat = Category;
            evt.Message = MaxLengthString(message, MaxLength);
            evt.Detail = MaxLengthString(detail, MaxLength);
            AddSingleEvent(evt);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    public void LogEvent(SystemEvent evt)
    {
        AddSingleEvent(evt);
        _sinkSeq = Math.Max(_sinkSeq, evt.SinkSeq + 1);
    }

    private void AddSingleEvent(SystemEvent evt)
    {
        Events.Enqueue(evt);
        _repo.RegisterEvent(evt);
        if (Events.Count > MaxMessages)
            Events.TryDequeue(out _);
    }

    /// <summary>
    /// If there is no detail but a massive message, put the whole message into detail
    /// and leave only the first line in message
    /// </summary>
    private void CleanMessageAndDetail(ref string message, ref string detail)
    {
        if (!string.IsNullOrEmpty(detail)) return;
        if (string.IsNullOrWhiteSpace(message)) return;

        int index = message.IndexOf("\n");
        if (index != -1)
        {
            detail = message;
            message = message.Substring(0, index);
        }
    }

    private static string MaxLengthString(string s, int maxLength)
    {
        if (s == null) return s;
        if (s.Length <= maxLength) return s;

        return s.Substring(0, maxLength);
    }

}