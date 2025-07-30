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
using System.Reflection;

namespace DiagnosticExplorer;

internal class DateGetter : PropertyGetter
{
    private bool _exposeDate = true;
    private bool _exposeElapsed = false;
    private bool _exposeTimeUntil = false;

    public DateGetter(PropertyInfo prop, DatePropertyAttribute attr, bool isStatic) : base(prop, isStatic)
    {
        if (attr != null)
        {
            _exposeDate = attr.ExposeDate;
            _exposeElapsed = attr.ExposeElapsed;
            _exposeTimeUntil = attr.ExposeTimeUntil;
        }
    }

    public override void GetProperties(object obj, PropertyBag bag, string catPrepend)
    {
        if (_exposeDate)
        {
            base.GetProperties(obj, bag, catPrepend);
        }

        var value = GetFunc(obj);
        DateTime? dateVal = value is DateTimeOffset off ? off.LocalDateTime : (DateTime?) value;
        if (dateVal != null && dateVal.Value.Kind == DateTimeKind.Utc)
            dateVal = dateVal.Value.ToLocalTime();

        if (_exposeElapsed)
        {
            string val = dateVal == null ? "" : FormatTimeSpan(DateTime.Now.Subtract(dateVal.Value));
            Property property = new Property("Time since " + Name, val);
            bag.AddProperty(property, PrependToCategory(catPrepend));
        }
        if (_exposeTimeUntil)
        {
            string val = dateVal == null ? "" : FormatTimeSpan(dateVal.Value.Subtract(DateTime.Now));
            Property property = new Property("Time until " + Name, val);
            bag.AddProperty(property, PrependToCategory(catPrepend));
        }
    }
}