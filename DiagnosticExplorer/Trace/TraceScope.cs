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
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using log4net;
using System.Linq;
using DiagnosticExplorer.Util;

namespace DiagnosticExplorer;

using ATraceItem = TraceItem<TraceScope>;

/// <summary>Enabled trace to a single source through method calls</summary>
public class TraceScope : IDisposable
{
    private static AsyncLocal<ScopeStack> _scopeStack = new();
    private static ReaderWriterLockSlim _scopeStackLock = new();
		
    private DateTime _created = DateTime.UtcNow;
    private DateTime? _disposed;
    private List<ATraceItem> _traceItems = new();
    private ReaderWriterLockSlim _traceItemsLock = new();
    private bool _forceTrace;
    private bool _isRoot;
    private Timer _autoTraceTimer;

    /// <summary>
    /// Sorted dictionary of time in milliseconds vs the trace method
    /// which should be used if the operation exceeds that time
    /// </summary>
    private SortedDictionary<int, Action<string>> _traceMethods;

    #region Constructors

    public TraceScope([CallerMemberName] string name = null)
    {
        Setup(name, null, false);
    }

    public TraceScope(Action<string> traceMethod, [CallerMemberName] string name = null)
    {
        Setup(name, traceMethod, false);
    }

    public TraceScope(string name, Action<string> traceMethod)
    {
        Setup(name, traceMethod, false);
    }
	
    public TraceScope(string name, Action<string> traceMethod, bool forceTrace)
    {
        Setup(name, traceMethod, forceTrace);
    }

    public TraceScope(Action<string> traceMethod, bool forceTrace, [CallerMemberName] string name = null)
    {
        Setup(name, traceMethod, forceTrace);
    }

    private void Setup(string name, Action<string> traceMethod, bool forceTrace)
    {
        _traceMethods = new SortedDictionary<int, Action<string>>();
        SetTraceAction(0, traceMethod);

        Name = name;

        _forceTrace = forceTrace;

        ScopeStack scopeStack = GetCurrentScopeStack(create: true);
        _isRoot = scopeStack.Current == null;
            
        TraceScope current = scopeStack.Current;
        current?.AddTraceItem(new ATraceItem(this));
        scopeStack.Add(this);
    }

    private void AddTraceItem(ATraceItem traceItem)
    {
        using (_traceItemsLock.WriteGuard())
            _traceItems.Add(traceItem);
    }

    #endregion
        

    public void StartAutoTraceTimer(TimeSpan time)
    {
        if (_autoTraceTimer != null)
            _autoTraceTimer.Dispose();

        _autoTraceTimer = new Timer(AutoTraceAfterTimeout, null, (int)time.TotalMilliseconds, Timeout.Infinite);
    }

    private void AutoTraceAfterTimeout(object state)
    {
        AddTraceItem(new ATraceItem("FORCE TRACE AFTER TIMEOUT"));

        TraceMessage();
    }

    public static TraceScope Current => GetCurrentScopeStack(create: false)?.Current;

    private static ScopeStack GetCurrentScopeStack(bool create)
    {
        ScopeStack scopeStack = _scopeStack.Value;
        if (scopeStack != null || !create)
            return scopeStack;

        using (_scopeStackLock.UpgradeableReadGuard())
        {
            if (_scopeStack.Value == null)
            {
                using (_scopeStackLock.WriteGuard())
                    _scopeStack.Value ??= new ScopeStack();
            }

            return _scopeStack.Value;
        }
    }


    public string Name { get; set; }
    public bool IsHidden { get; set; }

    public TimeSpan? SuppressDetailThreshold { get; set; }

    public void SetTraceAction(Action<string> traceMethod)
    {
        SetTraceAction(0, traceMethod);
    }

    public void SetTraceAction(int timeMillis, Action<string> traceMethod)
    {
        _traceMethods[timeMillis] = traceMethod;
    }

	
    public TimeSpan Age => DateTime.UtcNow.Subtract(_created);


    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        StringWriter writer = new StringWriter(sb);
        IndentedTextWriter indentedWriter = new IndentedTextWriter(writer);

        DateTime lastMessage = _created;
        ToString(indentedWriter, _created, ref lastMessage, 0);
        indentedWriter.Flush();
        writer.Flush();
        return sb.ToString();
    }

    public void ToString(IndentedTextWriter writer, DateTime scopeStart, ref DateTime lastMessage, int indent)
    {
        if (IsHidden) return;

        using (_traceItemsLock.ReadGuard())
        {
            if (_traceItems.Count == 0)
            {
                WriteBeginEndScope(this, writer, scopeStart, ref lastMessage, false);
            }
            else
            {
                WriteBeginScope(writer, scopeStart, ref lastMessage);

                writer.Indent += indent;
                foreach (ATraceItem item in _traceItems)
                {
                    if (item.TraceScope == null)
                    {
                        WriteString(writer, item.Message, item.Created, scopeStart, ref lastMessage);
                    }
                    else if (FullTraceRequired(item))
                    {
                        item.TraceScope.ToString(writer, scopeStart, ref lastMessage, 1);
                    }
                    else
                    {
                        WriteBeginEndScope(item.TraceScope, writer, scopeStart, ref lastMessage, true);
                    }
                }
                writer.Indent -= indent;

                WriteEndScope(writer, scopeStart, ref lastMessage);
            }
        }
    }

    private bool FullTraceRequired(ATraceItem item)
    {
        if (item.TraceScope == null) return false;
        if (item.TraceScope._forceTrace) return false;
        if (item.TraceScope.SuppressDetailThreshold == null) return true;

        TimeSpan thresh = item.TraceScope.SuppressDetailThreshold.Value;
        TimeSpan duration = item.TraceScope._disposed.Value - item.TraceScope._created;
        return duration >= thresh;
    }

    private void WriteBeginScope(IndentedTextWriter writer, DateTime scopeStart, ref DateTime lastMessage)
    {
        if (Name == null) return;

        string message = $"BEGIN {Name}";
        if (_disposed != null)
            message = string.Format("{0} ({1:N3} seconds)", message, _disposed.Value.Subtract(_created).TotalSeconds);

        WriteString(writer, message, _created, scopeStart, ref lastMessage);
    }

    private void WriteEndScope(IndentedTextWriter writer, DateTime scopeStart, ref DateTime lastMessage)
    {
        if (Name == null) return;
        if (_disposed == null) return;

        string message = string.Format("END {0} ({1:N3} seconds)", Name, _disposed.Value.Subtract(_created).TotalSeconds);

        WriteString(writer, message, _disposed.Value, scopeStart, ref lastMessage);
    }

    private static void WriteBeginEndScope(TraceScope scope, IndentedTextWriter writer,
        DateTime scopeStart, ref DateTime lastMessage, bool suppressed)
    {
        if (scope.Name == null) return;
        if (scope._disposed == null) return;

        TimeSpan duration = scope._disposed.Value.Subtract(scope._created);

        string message = string.Format("BEGIN/END{0} {1} ({2:N3} seconds)",
            suppressed ? "*" : "",
            scope.Name,
            duration.TotalSeconds);

        WriteString(writer, message, scope._disposed.Value, scopeStart, ref lastMessage);
    }

    private static void WriteString(IndentedTextWriter writer, string message, DateTime itemDate, DateTime scopeStart, ref DateTime lastMessage)
    {
        double age = itemDate.Subtract(scopeStart).TotalSeconds;
        double split = itemDate.Subtract(lastMessage).TotalSeconds;

        string[] parts = Regex.Split(message, @"\r?\n");
        writer.Write("[{0:00.000}] [{1:00.000}] ", age, split);
        writer.WriteLine(parts[0].Trim());

        for (int i = 1; i < parts.Length; i++)
        {
            writer.Write("                  ");
            writer.WriteLine(parts[i]);
        }

        lastMessage = itemDate;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {

            if (_autoTraceTimer != null)
            {
                _autoTraceTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _autoTraceTimer.Dispose();
                _autoTraceTimer = null;
            }

            _disposed = DateTime.UtcNow;

            var currentStack = GetCurrentScopeStack(create: false);
            if (currentStack != null)
            {
                currentStack.Remove(this);

                if (_scopeStack.Value == null)
                {
                    using (_scopeStackLock.WriteGuard())
                        _scopeStack.Value = null;
                }
            }

            TraceMessage();
        }
    }

    private void TraceMessage()
    {
        try
        {
            Action<string> action = GetTraceMethod(Age.TotalMilliseconds);
            if (action == null) return;
            if (!_isRoot && !_forceTrace) return;

            action(ToString());
        }
        catch (Exception ex)
        {
            LogManager.GetLogger(GetType()).Error(ex.Message, ex);
        }
    }

    private Action<string> GetTraceMethod(double millis)
    {
        foreach (var pair in _traceMethods.Reverse())
            if (millis >= pair.Key)
                return pair.Value;

        return _traceMethods.FirstOrDefault().Value;
    }

    #region Write Methods

    public static ITraceItem Trace(StackTrace trace)
    {
        string s = trace.ToString();
        if (s.Length > 200)
            s = s.Substring(0, 200);
        return Trace(s);
    }

    public static ITraceItem Trace(object o)
    {
        return Trace(o?.ToString() ?? "");
    }

    public static void TraceIf(bool condition, object o)
    {
        if (condition)
            Trace(o?.ToString() ?? "");
    }

    public static ITraceItem Trace(string message)
    {
        var current = Current;
        if (current == null)
            return null;

        ATraceItem item = new ATraceItem(message);
        current.AddTraceItem(item);
        return item;
    }

    public static void TraceIf(bool condition, string message)
    {
        if (condition)
            Trace(message);
    }
		
	
		
    #endregion
}