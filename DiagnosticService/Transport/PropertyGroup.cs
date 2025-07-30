﻿using System.Collections.Generic;

namespace Diagnostics.Service.Common.Transport;

public class PropertyGroup
{
    public string Name { get; set; }

    public Property[] Properties { get; set; }

    public static List<PropertyGroup> Map(string path, List<DiagnosticExplorer.Category> propertyGroups)
    {
        List<PropertyGroup> result = new();
        foreach (DiagnosticExplorer.Category group in propertyGroups)
        {
            PropertyGroup groupResult = new()
            {
                Name = group.Name ?? "General",
            };
            result.Add(groupResult);
            groupResult.Properties = Property.Map(path + "|" + group.Name ?? "", group.Properties).ToArray();
        }

        return result;
    }
}