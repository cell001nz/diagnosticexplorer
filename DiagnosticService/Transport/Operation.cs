﻿using System.Collections.Generic;
using System.Linq;
using DiagnosticExplorer;

namespace Diagnostics.Service.Common.Transport;

public class Operation
{
    public string ReturnType { get; set; }

    public string Signature { get; set; }

    public string Name { get; set; }

    public List<KeyValuePair<string, string>> Parameters { get; set; }

    public static List<Operation> GetOperationSet(string operationSetId, List<OperationSet> operationSets)
    {
        OperationSet operationSet = operationSets.FirstOrDefault(x => x.Id == operationSetId);
        if (operationSet == null)
            return new List<Operation>();

        return GetOperationSet(operationSet);
    }

    public static List<Operation> GetOperationSet(OperationSet operationSet)
    {
        List<Operation> result = new();
        operationSet.Operations.ForEach(op =>
        {
            result.Add(new Operation
            {
                ReturnType = op.ReturnType,
                Parameters = op.Parameters != null ? op.Parameters.Select(x => new KeyValuePair<string, string>(x.Name, x.Type)).ToList() : new List<KeyValuePair<string, string>>(),
                Signature = op.Signature,
                Name = op.Signature.Substring(0, op.Signature.IndexOf('('))
            });
        });

        return result;
    }


}