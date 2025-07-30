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
using System.Runtime.Serialization;
using ProtoBuf;

namespace DiagnosticExplorer;

/// <summary>
/// Describes something that happened.
/// </summary>
[ProtoContract(UseProtoMembersOnly = true)]
public class SystemEvent
{
        
    [ProtoMember(0)]
    public string Id { get; set; }
        
    [ProtoMember(0)]
    public string ProcessId { get; set; }
        
    [ProtoMember(1)]
    public long SinkSeq { get; set; }

    [ProtoMember(2)]
    public long ProcSeq { get; set; }

    [ProtoMember(3)]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [ProtoMember(4)]
    public string Message { get; set; }

    [ProtoMember(5)]
    public string Detail { get; set; }

    [ProtoMember(6)]
    public int Level { get; set; }

    [ProtoMember(7)]
    public string Sink { get; set; }

    [ProtoMember(8)]
    public string Cat { get; set; }

    public override string ToString()
    {
        return $"{ProcSeq} {SinkSeq} {Date:d MMM yyyy H:mm:ss} {Level} {Message}";
    }
}