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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DiagnosticExplorer.Util;

namespace DiagnosticExplorer;

internal class PropertyGetter
{
    public const int MaxConcatItems = 10;

    protected PropertyGetter()
    {
			
    }

    public PropertyGetter(PropertyInfo propInfo, bool isStatic)
    {
        PropInfo = propInfo;

        GetFunc = PropertyToFunction(propInfo, isStatic);
        Name = propInfo.Name;

        CategoryAttribute catAttr = AttributeUtil.GetAttribute<CategoryAttribute>(propInfo);
        if (catAttr != null)
            Category = catAttr.Category;

        DescriptionAttribute descAttr = AttributeUtil.GetAttribute<DescriptionAttribute>(propInfo);
        if (descAttr != null)
            Description = descAttr.Description;


        DiagnosticClassAttribute classAttr = propInfo.DeclaringType
            .GetCustomAttributes(typeof (DiagnosticClassAttribute), true)
            .Cast<DiagnosticClassAttribute>().
            FirstOrDefault();

        if (classAttr != null && classAttr.AllPropertiesSettable)
            CanSet = propInfo.CanWrite && classAttr.AllPropertiesSettable;

        PropertyAttribute propAttr = AttributeUtil.GetAttribute<PropertyAttribute>(propInfo);
        if (propAttr != null)
        {
            Name = propAttr.Name ?? Name;
            Category = propAttr.Category ?? Category;
            Description = propAttr.Description ?? Description;
            FormatString = propAttr.FormatString ?? GetDefaultFormatString(propInfo.PropertyType);
            if (propInfo.CanWrite && propAttr.AllowSetSpecified)
                CanSet = propAttr.AllowSet;
        }
    }

    protected Func<object, object> PropertyToFunction(PropertyInfo propInfo, bool isStatic)
    {
        if (propInfo == null) 
            return null;

        try
        {
            //return obj => propInfo.GetValue(obj, null);

            //This method takes 2/3 time of propInfo.GetValue
            if (isStatic)
                return obj => propInfo.GetValue(obj, null);

            ParameterExpression objParam = Expression.Parameter(typeof (object), "obj");
            UnaryExpression objToType = Expression.Convert(objParam, propInfo.DeclaringType);
            Expression propExp = Expression.Property(objToType, propInfo);
            Expression resultToObj = Expression.Convert(propExp, typeof (object));
            return (Func<object, object>) Expression.Lambda(resultToObj, objParam).Compile();
        }
        catch (Exception ex)
        {
            string msg = string.Format("Property {0}.{1}: {2}", propInfo.DeclaringType.Name, propInfo.Name, ex.Message);
            return obj => msg;
        }
    }

    protected Func<object, object> GetFunc { get; set; }

    protected static string MaxLengthString(string s, int maxLength)
    {
        if (s == null) return s;
        if (s.Length <= maxLength) return s;

        return s.Substring(0, maxLength);
    }

    public virtual void GetProperties(object obj, PropertyBag bag, string catPrepend)
    {
        Property p = new Property 
        {
            Name = Name,
            Description = Description,
            Value = MaxLengthString(GetValue(obj, out object objectValue), 8092),
            ValueObject = objectValue,
            CanSet = CanSet,
            SourceObject = obj,
            SourceProperty = PropInfo
        };

        string prependToCategory = PrependToCategory(catPrepend);
        bag.AddProperty(p, prependToCategory);
    }

    protected string PrependToCategory(string prepend)
    {
        return CombineCategories(prepend, Category);
    }

    protected static string CombineCategories(string start, string end)
    {
        if (string.IsNullOrEmpty(start))
            return end;
			
        if (string.IsNullOrEmpty(end))
            return start;

        return start + "." + end;
    }

    private string GetDefaultFormatString(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>))
            type = type.GetGenericArguments()[0];

        if (type == typeof(float))
            return "{0:N2}";

        if (type == typeof(double))
            return "{0:N2}";

        if (type == typeof(decimal))
            return "{0:N2}";

        if (type == typeof(DateTime) || type == typeof(DateTime?))
            return "{0:d MMM yyyy H:mm:ss}";

        return null;
    }

    public PropertyInfo PropInfo { get; private set; }
    public string Name { get; protected set; }
    public string Description { get; protected set; }
    protected string FormatString { get; private set; }
    public bool CanSet { get; private set; }
    public string Category { get; private set; }

    public string GetValue(object obj, out object objectValue)
    {
        return GetValue(obj, GetFunc, out objectValue);
    }

    protected string FormatEnumerable(IEnumerable col, string separator, int maxItems)
    {
        IEnumerable<object> asObject = col.Cast<object>();
        int count = asObject.Count();
        if (count == 0) 
            return "0 items";
	
        List<string> values = new List<string>();
        if (maxItems <= 0)
            maxItems = MaxConcatItems;

        int remaining = count - maxItems;

        foreach (object o in asObject.Take(maxItems))
            values.Add(FormatValue(o));

        if (remaining > 0)
            values.Add(string.Format("... ({0} more item{1})", remaining, remaining == 1 ? "" : "s"));

        string pre = string.Format("{0} item{1}: ", count, count == 1 ? "" : "s");
        return pre + string.Join(separator, values.ToArray());
    }

    public string GetValue(object obj, Func<object, object> propInfo, out object propertyValue)
    {
        try
        {
            propertyValue = propInfo(obj);
            if (propertyValue == null)
                return null;

            return FormatValue(propertyValue);
        }
        catch (Exception ex)
        {
            propertyValue = null;
            return string.Format("<{0}>", ex.Message);
        }
    }

    protected string FormatValue(object val)
    {
        if (val == null) 
            return null;

        if (val is TimeSpan)
            return FormatTimeSpan((TimeSpan)val);

        if (val is string)
            return (string)val;

        if (val is IEnumerable)
            return FormatEnumerable((IEnumerable)val, Environment.NewLine, MaxConcatItems);

        if (FormatString != null)
            return string.Format(FormatString, val);

        return val.ToString();
    }

    protected static string FormatTimeSpan(TimeSpan span)
    {
        string sign= span < TimeSpan.Zero ? "-" : "";
        string format = "{0}{2:D2}:{3:D2}:{4:D2}";
        if (span.Days != 0)
            format = "{0}{1}.{2:D2}:{3:D2}:{4:D2}";

        if (Math.Abs(span.TotalSeconds) < 1)
            format += ".{5:D2}";

        return string.Format(format, sign, 
            Math.Abs(span.Days), Math.Abs(span.Hours), 
            Math.Abs(span.Minutes), Math.Abs(span.Seconds), 
            Math.Abs(span.Milliseconds));
    }
}