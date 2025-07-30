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
using System.Collections.Generic;
using System.Linq;

namespace DiagnosticExplorer.Util;

public class WeakReferenceHash<T> where T : class
{
    private readonly IDictionary<string, WeakReference> items = new SortedDictionary<string, WeakReference>(StringComparer.CurrentCultureIgnoreCase);

    public void Add(string name, T obj)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (items.ContainsKey(name)) throw new ArgumentException(string.Format("There is already a {0} named '{1}'", typeof(T).Name, name));

        lock (items) items.Add(name, new WeakReference(obj));
    }

    public bool ContainsName(string name)
    {
        return items.ContainsKey(name);
    }

    public void Remove(string name)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));

        lock (items) items.Remove(name);
    }

    public T GetItem(string name, Func<T> create = null)
    {
        lock (items)
        {
            T target = null;
            if (items.TryGetValue(name, out WeakReference r))
            {
                target = (T) r.Target;
                if (target == null)
                    items.Remove(name);
            }

            if (target == null && create != null)
            {
                target = create();
                items.Add(name, new WeakReference(target));
            }

            return target;
        }
    }

    public List<T> GetItems()
    {
        lock (items)
        {
            List<T> toList = new(items.Count);
            foreach (KeyValuePair<string, WeakReference> pair in items.ToArray())
            {
                T t = (T) pair.Value.Target;
                if (t == null)
                    items.Remove(pair.Key);
                else
                    toList.Add(t);
            }

            return toList;
        }
    }
}