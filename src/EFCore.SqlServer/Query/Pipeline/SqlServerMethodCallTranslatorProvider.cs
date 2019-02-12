// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Relational.Query.PipeLine;

namespace Microsoft.EntityFrameworkCore.SqlServer.Query.Pipeline
{
    public class SqlServerMethodCallTranslatorProvider : RelationalMethodCallTranslatorProvider
    {
        private static readonly IMethodCallTranslator[] _methodCallTranslators =
        {
            new SqlServerStartsWithOptimizedTranslator()
        };

        public SqlServerMethodCallTranslatorProvider()
        {
            AddTranslators(_methodCallTranslators);
        }
    }
}
