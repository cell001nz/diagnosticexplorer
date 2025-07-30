﻿using System;

namespace DiagnosticExplorer.Common;

public class DiagServiceSettings
{
    public bool UseSpaProxy { get; set; }
    public string SpaDirectory { get; set; } = "";
    public string SpaProxy { get; set; } = "";
    public string RetroType { get; set; } = "";
    public string RetroConnection { get; set; } = "";
    public string[] Urls { get; set; } = [];

    public IRetroLogger CreateRetroLogger()
    {
        switch (RetroType.ToLower())
        {
            case "mongo":
                return new MongoRetroLogger(RetroConnection);

            default:
                throw new NotSupportedException($"ILogReader type {RetroType} not supported");
        }
    }
}