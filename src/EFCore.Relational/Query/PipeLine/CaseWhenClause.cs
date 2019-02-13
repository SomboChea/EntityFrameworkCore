// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class CaseWhenClause
    {
        public CaseWhenClause(Expression test, Expression result)
        {
            Test = test;
            Result = result;
        }

        public Expression Test { get; }
        public Expression Result { get; }
    }
}
