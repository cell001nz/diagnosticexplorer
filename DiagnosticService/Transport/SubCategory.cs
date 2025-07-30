﻿using System;
using DiagnosticExplorer;

namespace Diagnostics.Service.Common.Transport;

public class SubCategory
{
    public SubCategory()
    {
    }

    public SubCategory(PropertyBag subcategory)
    {
        SubCategory subCategoryModel = new()
        {
            Name = subcategory.Name,
            Path = (subcategory.Category + '|' + subcategory.Name),
        };
        subCategoryModel.PropertyGroups = PropertyGroup.Map(subCategoryModel.Path, subcategory.Categories).ToArray();
    }

    public string Name { get; set; }

    public PropertyGroup[] PropertyGroups { get; set; } = Array.Empty<PropertyGroup>();

    public SystemEvent[] Events { get; set; } = Array.Empty<SystemEvent>();

    public string Path { get; set; }

    public Operation[] Operations { get; set; } = Array.Empty<Operation>();

}