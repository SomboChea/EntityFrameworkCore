// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.Relational.Query.PipeLine
{
    public class CaseExpression : Expression
    {
        public CaseExpression(IEnumerable<CaseWhenClause> whenClauses, Expression elseResult)
        {
            WhenClauses = whenClauses;
            ElseResult = elseResult;
        }

        public override ExpressionType NodeType => ExpressionType.Extension;
        public override Type Type => WhenClauses.Select(wc => wc.Result.Type).FirstOrDefault();
        public IEnumerable<CaseWhenClause> WhenClauses { get; }
        public Expression ElseResult { get; }
    }
}
